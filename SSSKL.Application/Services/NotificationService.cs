using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;


namespace SSSKL.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IPushSubscriptionRepository _pushSubscriptionRepository;
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly IWebPushService _webPushService;

    public NotificationService(
        INotificationRepository notificationRepository,
        IPushSubscriptionRepository pushSubscriptionRepository,
        IApplicationUserRepository applicationUserRepository,
        IWebPushService webPushService)
    {
        _notificationRepository = notificationRepository;
        _pushSubscriptionRepository = pushSubscriptionRepository;
        _applicationUserRepository = applicationUserRepository;
        _webPushService = webPushService;
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(string userId, bool unreadOnly, int skip, int take)
    {
        var notifications = await _notificationRepository.GetForUser(userId, unreadOnly, skip, take);

        return notifications
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                LinkUri = n.LinkUri,
                CreatedOn = n.CreatedOn
            })
            .ToList();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _notificationRepository.GetUnreadCount(userId);
    }

    public async Task MarkAsReadAsync(Guid id, string userId)
    {
        await _notificationRepository.MarkAsRead(id, userId);
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _notificationRepository.MarkAllAsRead(userId);
    }

    public async Task CreateNotificationAsync(string userId, string title, string message, string? linkUri = null, bool sendPush = false)
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

        await _notificationRepository.AddRange([notification]);

        if (sendPush)
        {
            await _webPushService.SendNotificationAsync(userId, title, message, linkUri);
        }
    }

    public async Task CreateCustomNotificationAsync(CreateCustomNotificationDto dto)
    {
        IEnumerable<string> targetUserIds;

        if (dto.FanOut)
        {
            targetUserIds = await _applicationUserRepository.GetAllIds();
        }
        else
        {
            targetUserIds = dto.UserIds ?? new List<string>();
        }

        var notifications = targetUserIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = dto.Title,
            Message = dto.Message,
            LinkUri = dto.LinkUri,
            IsRead = false,
            CreatedOn = DateTime.UtcNow
        }).ToList();

        await _notificationRepository.AddRange(notifications);

        if (dto.SendPush)
        {
            foreach (var userId in targetUserIds)
            {
                await _webPushService.SendNotificationAsync(userId, dto.Title, dto.Message, dto.LinkUri);
            }
        }
    }

    public async Task SubscribeAsync(string userId, PushSubscriptionDto dto)
    {
        await _pushSubscriptionRepository.Upsert(new PushSubscription
        {
            UserId = userId,
            Endpoint = dto.Endpoint,
            P256dh = dto.P256dh,
            Auth = dto.Auth,
            CreatedOn = DateTime.UtcNow
        });
    }

    public async Task UnsubscribeAsync(string userId, string endpoint)
    {
        await _pushSubscriptionRepository.Delete(userId, endpoint);
    }
}
