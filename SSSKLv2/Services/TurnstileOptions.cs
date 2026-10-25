namespace SSSKLv2.Services;

public class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    /// <summary>Widget secret key. Leave empty to disable verification (local development, tests).</summary>
    public string? SecretKey { get; set; }

    /// <summary>Frontend hostnames the widget may be solved on. Never include localhost in production.</summary>
    public string[] AllowedHostnames { get; set; } = [];
}
