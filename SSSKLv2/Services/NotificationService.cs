using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Dto;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(string userId, bool unreadOnly, int skip, int take)
    {
        var query = _context.Notification
            .Where(n => n.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedOn)
            .Skip(skip)
            .Take(take)
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
            .ToListAsync();

        return notifications;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notification
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(Guid id, string userId)
    {
        var notification = await _context.Notification
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var unreadNotifications = await _context.Notification
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Any())
        {
            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task CreateNotificationAsync(string userId, string title, string message, string? linkUri = null)
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

        _context.Notification.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task CreateCustomNotificationAsync(CreateCustomNotificationDto dto)
    {
        IEnumerable<string> targetUserIds;

        if (dto.FanOut)
        {
            targetUserIds = await _context.Users.Select(u => u.Id).ToListAsync();
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
        });

        _context.Notification.AddRange(notifications);
        await _context.SaveChangesAsync();
    }
}
