using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class AnnouncementService(IAnnouncementRepository announcementRepository) : IAnnouncementService
{
    public Task<IQueryable<Announcement>> GetAllAnnouncements()
    {
        return announcementRepository.GetAll();
    }

    // public Task<IEnumerable<Announcement>> GetAllAnnouncementsForEnduser()
    // {
    //     return announcementRepository.GetAllForEnduser();
    // }

    public Task<Announcement> GetAnnouncementById(Guid id)
    {
        return announcementRepository.GetById(id);
    }

    public Task CreateAnnouncement(Announcement announcement)
    {
        return announcementRepository.Create(announcement);
    }

    public Task UpdateAnnouncement(Announcement announcement)
    {
        return announcementRepository.Update(announcement);
    }

    public Task DeleteAnnouncement(Guid id)
    {
        return announcementRepository.Delete(id);
    }
}