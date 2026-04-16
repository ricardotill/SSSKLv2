using SSSKLv2.Dto;

namespace SSSKLv2.Services.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(string userId, bool unreadOnly, int skip, int take);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(Guid id, string userId);
    Task MarkAllAsReadAsync(string userId);
    Task CreateNotificationAsync(string userId, string title, string message, string? linkUri = null, bool sendPush = false);
    Task CreateCustomNotificationAsync(CreateCustomNotificationDto dto);
    Task SubscribeAsync(string userId, PushSubscriptionDto dto);
    Task UnsubscribeAsync(string userId, string endpoint);
}
