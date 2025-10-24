using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IAnnouncementService
{
    Task<int> GetCount();
    Task<IList<Announcement>> GetAllAnnouncements();
    Task<IList<Announcement>> GetAllAnnouncements(int skip, int take);
    IQueryable<Announcement> GetAllAnnouncementsQueryable(ApplicationDbContext context);
    Task<Announcement?> GetAnnouncementById(Guid id);
    Task CreateAnnouncement(Announcement announcement);
    Task UpdateAnnouncement(Announcement announcement);
    Task DeleteAnnouncement(Guid id);
}