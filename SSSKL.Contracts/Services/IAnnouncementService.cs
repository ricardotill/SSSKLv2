using SSSKL.Domain.Entities;


namespace SSSKL.Contracts.Services;

public interface IAnnouncementService
{
    Task<int> GetCount();
    Task<IList<Announcement>> GetAllAnnouncements();
    Task<IList<Announcement>> GetAllAnnouncements(int skip, int take);
    Task<Announcement?> GetAnnouncementById(Guid id);
    Task CreateAnnouncement(Announcement announcement);
    Task UpdateAnnouncement(Announcement announcement);
    Task DeleteAnnouncement(Guid id);
}