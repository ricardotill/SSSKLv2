namespace SSSKLv2.Services.Interfaces;

public interface IWebPushService
{
    Task SendNotificationAsync(string userId, string title, string message, string? url = null);
}
