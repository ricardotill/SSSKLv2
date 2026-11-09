using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using SSSKL.Domain.Entities;

namespace SSSKL.Web.Test.Integration.Infrastructure.Fakes;

public sealed record CapturedEmail(string Kind, string To, string Content);

/// <summary>Captures identity emails so tests can follow confirmation and reset links.</summary>
public sealed class FakeEmailSender : IEmailSender<ApplicationUser>
{
    public const string Confirm = "confirm";
    public const string ResetLink = "reset-link";
    public const string ResetCode = "reset-code";

    private readonly ConcurrentQueue<CapturedEmail> _sent = new();

    public IReadOnlyCollection<CapturedEmail> Sent => _sent.ToArray();

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        Capture(Confirm, email, WebUtility.HtmlDecode(confirmationLink));

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        Capture(ResetLink, email, WebUtility.HtmlDecode(resetLink));

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        Capture(ResetCode, email, resetCode);

    public CapturedEmail? LastFor(string email, string kind) =>
        _sent.LastOrDefault(e => e.Kind == kind && string.Equals(e.To, email, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyDictionary<string, string> Query(string link) =>
        QueryHelpers.ParseQuery(new Uri(link).Query).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

    private Task Capture(string kind, string to, string content)
    {
        _sent.Enqueue(new CapturedEmail(kind, to, content));
        return Task.CompletedTask;
    }
}
