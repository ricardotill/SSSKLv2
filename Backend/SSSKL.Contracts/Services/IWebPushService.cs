

namespace SSSKL.Contracts.Services;

public interface IWebPushService
{
    Task SendNotificationAsync(string userId, string title, string message, string? url = null);
}
