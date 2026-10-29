using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Shared;

internal sealed class UserNotifier(
    INotificationRepository notificationRepository,
    IWebPushService webPushService) : IUserNotifier
{
    public async Task NotifyAsync(string userId, string title, string message, string? linkUri = null, bool sendPush = false)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            LinkUri = linkUri,
            IsRead = false,
            CreatedOn = DateTime.UtcNow
        };

        await notificationRepository.AddRange([notification]);

        if (sendPush)
        {
            await webPushService.SendNotificationAsync(userId, title, message, linkUri);
        }
    }
}
