using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IAnnouncementService
{
    Task<IEnumerable<Announcement>> GetAllAnnouncements();
    IQueryable<Announcement> GetAllAnnouncementsQueryable(ApplicationDbContext context);
    Task<Announcement?> GetAnnouncementById(Guid id);
    Task CreateAnnouncement(Announcement announcement);
    Task UpdateAnnouncement(Announcement announcement);
    Task DeleteAnnouncement(Guid id);
}