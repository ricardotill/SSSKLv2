namespace SSSKLv2.Data.Constants;

public static class GlobalSettingsKeys
{
    public const string EmailSmtpServer = "EmailSmtpServer";
    public const string EmailSmtpPort = "EmailSmtpPort";
    public const string EmailSenderName = "EmailSenderName";
    public const string EmailSenderEmail = "EmailSenderEmail";
    public const string EmailSmtpUsername = "EmailSmtpUsername";
    public const string EmailSmtpPassword = "EmailSmtpPassword";

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
}
