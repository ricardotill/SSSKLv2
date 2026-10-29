namespace SSSKL.Application.Shared;

/// <summary>Stores in-app notifications for users and optionally sends them as web push.</summary>
public interface IUserNotifier
{
    Task NotifyAsync(string userId, string title, string message, string? linkUri = null, bool sendPush = false);

    /// <summary>Notifies the given users, or every user when <paramref name="userIds"/> is null.</summary>
    Task NotifyManyAsync(IEnumerable<string>? userIds, string title, string message, string? linkUri = null, bool sendPush = false);
}
