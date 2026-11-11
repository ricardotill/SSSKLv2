using System.Collections.Concurrent;
using SSSKL.Contracts.Services;

namespace SSSKL.Web.Test.Integration.Infrastructure.Fakes;

public sealed record SentPush(string UserId, string Title, string Message, string? Url);

/// <summary>Records push notifications instead of calling a push service.</summary>
public sealed class FakeWebPushService : IWebPushService
{
    private readonly ConcurrentQueue<SentPush> _sent = new();

    public IReadOnlyCollection<SentPush> Sent => _sent.ToArray();

    public Task SendNotificationAsync(string userId, string title, string message, string? url = null)
    {
        _sent.Enqueue(new SentPush(userId, title, message, url));
        return Task.CompletedTask;
    }
}
