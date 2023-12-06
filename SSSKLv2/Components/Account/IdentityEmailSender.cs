using Microsoft.AspNetCore.Identity;
using SendGrid;
using SendGrid.Helpers.Mail;
using SSSKLv2.Data;

namespace SSSKLv2.Components.Account;

public class IdentityEmailSender(ILogger<IdentityEmailSender> logger) : IEmailSender<ApplicationUser>
{
    private readonly ILogger _logger = logger;

    public string? SendGridKey { get; } = Environment.GetEnvironmentVariable("SENDGRID_KEY");

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        if (string.IsNullOrEmpty(SendGridKey))
        {
            throw new Exception("Null SendGridKey");
        }
        await Execute(SendGridKey, subject, message, toEmail);
    }

    public async Task Execute(string? apiKey, string subject, string message, string toEmail)
    {
        var client = new SendGridClient(apiKey);
        var msg = new SendGridMessage()
        {
            From = new EmailAddress("webmaster@scoutingwilo.nl", "Wilo Webmaster"),
            Subject = subject,
            PlainTextContent = message,
            HtmlContent = message
        };
        msg.AddTo(new EmailAddress(toEmail));

        // Disable click tracking.
        // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
        msg.SetClickTracking(false, false);
        var response = await client.SendEmailAsync(msg);
        _logger.LogInformation(response.IsSuccessStatusCode 
            ? $"Email to {toEmail} queued successfully!"
            : $"Failure Email to {toEmail}");
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendEmailAsync(email, "Bevestig jouw account", $"Bevestig jouw account door <a href='{confirmationLink}'>hier te klikken</a>.");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendEmailAsync(email, "Reset jouw wachtwoord", $"Reset jouw account door <a href='{resetLink}'>hier te klikken</a>.");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendEmailAsync(email, "Reset jouw wachtwoord", $"Reset jouw wachtwoord met behulp van de volgende code: {resetCode}");
}