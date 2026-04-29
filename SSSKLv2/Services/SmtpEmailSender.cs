using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SSSKLv2.Data;
using SSSKLv2.Data.Constants;

namespace SSSKLv2.Services;

public class SmtpEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<SmtpEmailSender> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        await SendEmailAsync(email, "Bevestig je e-mailadres",
            GetHtmlTemplate("Welkom bij SSSKL!",
                $"Bedankt voor het registreren. Klik op de onderstaande knop om je e-mailadres te bevestigen en je account te activeren.",
                confirmationLink, "E-mailadres bevestigen"));

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        await SendEmailAsync(email, "Wachtwoord herstellen",
            GetHtmlTemplate("Wachtwoord herstellen",
                "Je hebt een verzoek ingediend om je wachtwoord te herstellen. Klik op de onderstaande knop om een nieuw wachtwoord in te stellen.",
                resetLink, "Wachtwoord herstellen"));

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        await SendEmailAsync(email, "Je verificatiecode",
            GetHtmlTemplate("Verificatiecode",
                $"Gebruik de onderstaande code om je wachtwoord te herstellen. Deze code is beperkt geldig.<br/><br/><div style='font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #2563eb;'>{resetCode}</div>",
                null, null));

    private string GetHtmlTemplate(string title, string message, string? buttonUrl = null, string? buttonText = null)
    {
        // Emerald palette — matches the frontend PrimeNG Aura preset
        // primary-500: #10b981  primary-600: #059669  primary-800: #065f46
        var buttonHtml = !string.IsNullOrEmpty(buttonUrl) && !string.IsNullOrEmpty(buttonText)
            ? $@"<div style='margin-top: 30px;'>
                    <a href='{buttonUrl}' style='background-color: #059669; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 15px; display: inline-block; letter-spacing: 0.01em;'>{buttonText}</a>
                </div>"
            : "";

        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        </head>
        <body style=""font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f0fdf4; margin: 0; padding: 40px 20px;"">
            <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px -1px rgba(16, 185, 129, 0.12);"">
                <div style=""background-color: #059669; background: linear-gradient(135deg, #065f46 0%, #059669 100%); padding: 36px 30px; text-align: center;"">
                    <div style=""display: inline-block; background-color: rgba(255,255,255,0.15); color: white; font-size: 12px; font-weight: 600; letter-spacing: 0.1em; text-transform: uppercase; padding: 4px 12px; border-radius: 20px; margin-bottom: 12px;"">Scouting Wilo</div>
                    <h1 style=""color: white; margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.025em;"">🌿 SSSKL</h1>
                </div>
                <div style=""padding: 44px 40px; text-align: center; line-height: 1.7; color: #374151;"">
                    <h2 style=""color: #065f46; margin-top: 0; margin-bottom: 12px; font-size: 22px; font-weight: 700;"">{title}</h2>
                    <p style=""margin: 0 0 16px; font-size: 15px; color: #4b5563;"">{message}</p>
                    {buttonHtml}
                </div>
                <div style=""height: 1px; background-color: #d1fae5; margin: 0;""></div>
                <div style=""padding: 24px; text-align: center; color: #9ca3af; font-size: 13px; background-color: #f9fafb;"">
                    &copy; {DateTime.UtcNow.Year} Scouting Wilo &mdash; Alle rechten voorbehouden.<br/>
                    <a href='https://ssskl.scoutingwilo.nl' style=""color: #059669; text-decoration: none;"">ssskl.scoutingwilo.nl</a>
                </div>
            </div>
        </body>
        </html>";
    }

    private async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.GlobalSetting
                .Where(s => GlobalSettingsKeys.SensitiveKeys.Contains(s.Key))
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            if (!settings.TryGetValue(GlobalSettingsKeys.EmailSmtpServer, out var smtpServer) ||
                !settings.TryGetValue(GlobalSettingsKeys.EmailSmtpPort, out var smtpPortStr) ||
                !settings.TryGetValue(GlobalSettingsKeys.EmailSmtpUsername, out var username) ||
                !settings.TryGetValue(GlobalSettingsKeys.EmailSmtpPassword, out var password) ||
                !settings.TryGetValue(GlobalSettingsKeys.EmailSenderEmail, out var senderEmail))
            {
                _logger.LogWarning("Email settings are not fully configured in GlobalSettings. Missing one or more required keys.");
                return;
            }

            // Trim inputs to prevent DNS resolution errors caused by leading/trailing whitespace
            smtpServer = smtpServer.Trim();
            smtpPortStr = smtpPortStr.Trim();
            username = username.Trim();
            password = password.Trim();
            senderEmail = senderEmail.Trim();

            if (string.IsNullOrEmpty(smtpServer))
            {
                _logger.LogError("SMTP Server hostname is empty.");
                return;
            }

            if (!int.TryParse(smtpPortStr, out var smtpPort))
            {
                _logger.LogError("Invalid SMTP port configured: '{Port}'", smtpPortStr);
                return;
            }

            var senderName = settings.GetValueOrDefault(GlobalSettingsKeys.EmailSenderName, "SSSKLv2")?.Trim() ?? "SSSKLv2";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            // Log connection attempt details (excluding password)
            _logger.LogInformation("Attempting to connect to SMTP server: {Host}:{Port} as {User}", smtpServer, smtpPort, username);

            var security = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;

            await client.ConnectAsync(smtpServer, smtpPort, security);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {Email}", email);
        }
        catch (System.Net.Sockets.SocketException socketEx)
        {
            _logger.LogError(socketEx, "DNS or Network error connecting to SMTP server. Ensure the hostname is correct and accessible.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed failure sending email to {Email}", email);
            throw;
        }
    }
}
