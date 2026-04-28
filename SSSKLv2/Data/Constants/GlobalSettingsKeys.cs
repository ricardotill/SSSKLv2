namespace SSSKLv2.Data.Constants;

public static class GlobalSettingsKeys
{
    public const string EmailSmtpServer = "EmailSmtpServer";
    public const string EmailSmtpPort = "EmailSmtpPort";
    public const string EmailSenderName = "EmailSenderName";
    public const string EmailSenderEmail = "EmailSenderEmail";
    public const string EmailSmtpUsername = "EmailSmtpUsername";
    public const string EmailSmtpPassword = "EmailSmtpPassword";

    /// <summary>
    /// Keys that are sensitive: they can be stored and used internally, but
    /// can only be retrieved by admins.
    /// </summary>
    public static readonly IReadOnlyList<string> SensitiveKeys = new List<string>
    {
        EmailSmtpServer,
        EmailSmtpPort,
        EmailSenderName,
        EmailSenderEmail,
        EmailSmtpUsername,
        EmailSmtpPassword,
        "GoogleMapsApiKey" // Existing sensitive key found in controller
    };

    /// <summary>
    /// Keys that are write-only: they can be stored and used internally, but
    /// must NEVER be returned via the API — not even to admins.
    /// </summary>
    public static readonly IReadOnlyList<string> WriteOnlyKeys = new List<string>
    {
        EmailSmtpPassword
    };
}
