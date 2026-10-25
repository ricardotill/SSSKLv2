namespace SSSKLv2.Services.Interfaces;

public interface ITurnstileService
{
    /// <summary>
    /// What the frontend needs to render the widget. Enabled only when the admin switched Turnstile on
    /// in Global Settings and both the sitekey and secret are filled in.
    /// </summary>
    Task<TurnstileClientConfig> GetClientConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Redeems a Turnstile token at Cloudflare Siteverify and checks success, action and hostname.
    /// Passes without a call when Turnstile is disabled. Tokens are single-use.
    /// </summary>
    Task<TurnstileVerificationResult> VerifyAsync(string? token, string expectedAction, string? remoteIp, CancellationToken cancellationToken = default);
}

public sealed record TurnstileClientConfig(bool Enabled, string? SiteKey);

public sealed record TurnstileVerificationResult(bool Success, string? FailureReason = null)
{
    public static TurnstileVerificationResult Passed { get; } = new(true);
    public static TurnstileVerificationResult Failed(string reason) => new(false, reason);
}
