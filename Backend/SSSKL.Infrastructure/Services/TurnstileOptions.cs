namespace SSSKL.Infrastructure.Services;

public class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    /// <summary>
    /// Default frontend hostnames the widget may be solved on, used when the TurnstileAllowedHostnames
    /// global setting is empty. Never include localhost in production.
    /// </summary>
    public string[] AllowedHostnames { get; set; } = [];
}
