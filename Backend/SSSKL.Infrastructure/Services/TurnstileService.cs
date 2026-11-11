using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Infrastructure.Persistence;

namespace SSSKL.Infrastructure.Services;

/// <summary>
/// Server-side Cloudflare Turnstile verification, configured in Global Settings. The browser only obtains
/// a token; it is redeemed here, never from the client, so the secret key stays on the server.
/// </summary>
public class TurnstileService(
    HttpClient httpClient,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IMemoryCache cache,
    IOptions<TurnstileOptions> options,
    ILogger<TurnstileService> logger) : ITurnstileService
{
    public const string SiteverifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    // Settings are edited in Global Settings; a short cache keeps login traffic off the settings table
    // while letting an admin's change apply within half a minute.
    private const string CacheKey = "turnstile-settings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    // Tokens are at most 2048 characters per Cloudflare's documentation
    private const int MaxTokenLength = 2048;

    private static readonly string[] SettingKeys =
    [
        GlobalSettingsKeys.TurnstileEnabled,
        GlobalSettingsKeys.TurnstileSiteKey,
        GlobalSettingsKeys.TurnstileSecretKey,
        GlobalSettingsKeys.TurnstileAllowedHostnames
    ];

    public async Task<TurnstileClientConfig> GetClientConfigAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return new TurnstileClientConfig(settings.Enabled, settings.Enabled ? settings.SiteKey : null);
    }

    public async Task<TurnstileVerificationResult> VerifyAsync(string? token, string expectedAction, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        if (!settings.Enabled) return TurnstileVerificationResult.Passed;

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
        {
            return TurnstileVerificationResult.Failed("missing-input-response");
        }

        var form = new Dictionary<string, string>
        {
            ["secret"] = settings.SecretKey!,
            ["response"] = token,
            // Lets the HTTP resilience handler retry a timed-out call without the retry being
            // rejected as an already redeemed token
            ["idempotency_key"] = Guid.NewGuid().ToString()
        };
        if (!string.IsNullOrWhiteSpace(remoteIp)) form["remoteip"] = remoteIp;

        SiteverifyResponse? result;
        try
        {
            using var response = await httpClient.PostAsync(SiteverifyUrl, new FormUrlEncodedContent(form), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Turnstile siteverify returned HTTP {StatusCode}", (int)response.StatusCode);
                return TurnstileVerificationResult.Failed("siteverify-unavailable");
            }
            result = await response.Content.ReadFromJsonAsync<SiteverifyResponse>(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            // Fail closed: without a verdict the request is treated as unverified
            logger.LogError(ex, "Turnstile siteverify call failed");
            return TurnstileVerificationResult.Failed("siteverify-unavailable");
        }

        if (result is null || !result.Success)
        {
            var codes = result?.ErrorCodes ?? [];
            if (codes.Contains("invalid-input-secret") || codes.Contains("missing-input-secret"))
            {
                // A wrong secret in Global Settings is an admin mistake, not a bot. Letting requests through
                // keeps admins able to sign in and fix it; the error log makes the misconfiguration visible.
                logger.LogError("Turnstile secret key in Global Settings was rejected by Cloudflare; verification is skipped until it is fixed");
                return TurnstileVerificationResult.Passed;
            }
            var joined = codes.Length > 0 ? string.Join(",", codes) : "unknown";
            logger.LogWarning("Turnstile verification failed for action {Action}: {ErrorCodes}", expectedAction, joined);
            return TurnstileVerificationResult.Failed(joined);
        }

        if (!string.Equals(result.Action, expectedAction, StringComparison.Ordinal))
        {
            logger.LogWarning("Turnstile action mismatch: expected {Expected}, got {Actual}", expectedAction, result.Action);
            return TurnstileVerificationResult.Failed("action-mismatch");
        }

        if (!settings.AllowedHostnames.Contains(result.Hostname, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning("Turnstile hostname {Hostname} is not in the allowed list", result.Hostname);
            return TurnstileVerificationResult.Failed("hostname-mismatch");
        }

        return TurnstileVerificationResult.Passed;
    }

    private async Task<TurnstileSettings> GetSettingsAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out TurnstileSettings? cached) && cached is not null) return cached;

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var values = await context.GlobalSetting
            .Where(s => SettingKeys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value.Trim(), cancellationToken);

        var siteKey = values.GetValueOrDefault(GlobalSettingsKeys.TurnstileSiteKey);
        var secretKey = values.GetValueOrDefault(GlobalSettingsKeys.TurnstileSecretKey);
        var switchedOn = bool.TryParse(values.GetValueOrDefault(GlobalSettingsKeys.TurnstileEnabled), out var on) && on;
        var hostnames = values.GetValueOrDefault(GlobalSettingsKeys.TurnstileAllowedHostnames)?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var settings = new TurnstileSettings(
            Enabled: switchedOn && !string.IsNullOrEmpty(siteKey) && !string.IsNullOrEmpty(secretKey),
            SiteKey: siteKey,
            SecretKey: secretKey,
            AllowedHostnames: hostnames is { Length: > 0 } ? hostnames : options.Value.AllowedHostnames);

        cache.Set(CacheKey, settings, CacheDuration);
        return settings;
    }

    private sealed record TurnstileSettings(bool Enabled, string? SiteKey, string? SecretKey, string[] AllowedHostnames);

    private sealed class SiteverifyResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("hostname")] public string? Hostname { get; set; }
        [JsonPropertyName("error-codes")] public string[]? ErrorCodes { get; set; }
    }
}
