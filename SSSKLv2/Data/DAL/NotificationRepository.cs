using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    public async Task<IList<Notification>> GetForUser(string userId, bool unreadOnly, int skip, int take)
    {
        var query = dbContext.Notification
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCount(string userId)
    {
        return await dbContext.Notification
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkAsRead(Guid id, string userId)
    {
        var notification = await dbContext.Notification
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsRead(string userId)
    {
        await dbContext.Notification
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task AddRange(IEnumerable<Notification> notifications)
    {
        dbContext.Notification.AddRange(notifications);
        await dbContext.SaveChangesAsync();
    }
}
