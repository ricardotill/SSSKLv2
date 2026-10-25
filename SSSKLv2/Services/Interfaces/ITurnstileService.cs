namespace SSSKLv2.Services.Interfaces;

public interface ITurnstileService
{
    /// <summary>False when no secret key is configured; verification is then skipped.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Redeems a Turnstile token at Cloudflare Siteverify and checks success, action and hostname.
    /// Tokens are single-use: a second call with the same token fails.
    /// </summary>
    Task<TurnstileVerificationResult> VerifyAsync(string? token, string expectedAction, string? remoteIp, CancellationToken cancellationToken = default);
}

public sealed record TurnstileVerificationResult(bool Success, string? FailureReason = null)
{
    public static TurnstileVerificationResult Passed { get; } = new(true);
    public static TurnstileVerificationResult Failed(string reason) => new(false, reason);
}
