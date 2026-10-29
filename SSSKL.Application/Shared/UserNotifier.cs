using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Shared;

internal sealed class UserNotifier(
    INotificationRepository notificationRepository,
    IApplicationUserRepository applicationUserRepository,
    IWebPushService webPushService) : IUserNotifier
{
    public Task NotifyAsync(string userId, string title, string message, string? linkUri = null, bool sendPush = false)
        => NotifyManyAsync([userId], title, message, linkUri, sendPush);

    public async Task NotifyManyAsync(IEnumerable<string>? userIds, string title, string message, string? linkUri = null, bool sendPush = false)
    {
        var targetUserIds = (userIds ?? await applicationUserRepository.GetAllIds()).ToList();

        var notifications = targetUserIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            LinkUri = linkUri,
            IsRead = false,
            CreatedOn = DateTime.UtcNow
        }).ToList();

        await notificationRepository.AddRange(notifications);

        if (sendPush)
        {
            foreach (var userId in targetUserIds)
            {
                await webPushService.SendNotificationAsync(userId, title, message, linkUri);
            }
        }
    }
}
