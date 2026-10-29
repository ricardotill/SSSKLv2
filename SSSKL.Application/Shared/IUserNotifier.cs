namespace SSSKL.Application.Shared;

/// <summary>Stores an in-app notification for a user and optionally sends it as a web push.</summary>
public interface IUserNotifier
{
    Task NotifyAsync(string userId, string title, string message, string? linkUri = null, bool sendPush = false);
}
