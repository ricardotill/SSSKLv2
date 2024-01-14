using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IAnnouncementService
{
    Task<IQueryable<Announcement>> GetAllAnnouncements();
    // Task<IEnumerable<Announcement>> GetAllAnnouncementsForEnduser();
    Task<Announcement> GetAnnouncementById(Guid id);
    Task CreateAnnouncement(Announcement announcement);
    Task UpdateAnnouncement(Announcement announcement);
    Task DeleteAnnouncement(Guid id);
}