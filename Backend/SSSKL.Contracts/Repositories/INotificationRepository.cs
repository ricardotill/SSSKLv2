using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Repositories;

public interface INotificationRepository
{
    Task<IList<Notification>> GetForUser(string userId, bool unreadOnly, int skip, int take);
    Task<int> GetUnreadCount(string userId);
    Task MarkAsRead(Guid id, string userId);
    Task MarkAllAsRead(string userId);
    Task AddRange(IEnumerable<Notification> notifications);
}
