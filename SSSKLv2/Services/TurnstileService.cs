using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

/// <summary>
/// Server-side Cloudflare Turnstile verification. The browser only obtains a token; it is redeemed here,
/// never from the client, so the secret key stays on the server.
/// </summary>
public class TurnstileService(
    HttpClient httpClient,
    IOptions<TurnstileOptions> options,
    ILogger<TurnstileService> logger) : ITurnstileService
{
    public const string SiteverifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    // Tokens are at most 2048 characters per Cloudflare's documentation
    private const int MaxTokenLength = 2048;

    private readonly TurnstileOptions _options = options.Value;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.SecretKey);

    public async Task<TurnstileVerificationResult> VerifyAsync(string? token, string expectedAction, string? remoteIp, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled) return TurnstileVerificationResult.Passed;

        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
        {
            return TurnstileVerificationResult.Failed("missing-input-response");
        }

        var form = new Dictionary<string, string>
        {
            ["secret"] = _options.SecretKey!,
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
            var codes = result?.ErrorCodes is { Length: > 0 } c ? string.Join(",", c) : "unknown";
            if (codes.Contains("invalid-input-secret") || codes.Contains("missing-input-secret"))
            {
                logger.LogError("Turnstile secret key was rejected by Cloudflare ({ErrorCodes})", codes);
            }
            else
            {
                logger.LogWarning("Turnstile verification failed for action {Action}: {ErrorCodes}", expectedAction, codes);
            }
            return TurnstileVerificationResult.Failed(codes);
        }

        if (!string.Equals(result.Action, expectedAction, StringComparison.Ordinal))
        {
            logger.LogWarning("Turnstile action mismatch: expected {Expected}, got {Actual}", expectedAction, result.Action);
            return TurnstileVerificationResult.Failed("action-mismatch");
        }

        if (!_options.AllowedHostnames.Contains(result.Hostname, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning("Turnstile hostname {Hostname} is not in the allowed list", result.Hostname);
            return TurnstileVerificationResult.Failed("hostname-mismatch");
        }

        return TurnstileVerificationResult.Passed;
    }

    private sealed class SiteverifyResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("action")] public string? Action { get; set; }
        [JsonPropertyName("hostname")] public string? Hostname { get; set; }
        [JsonPropertyName("error-codes")] public string[]? ErrorCodes { get; set; }
    }
}
