using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class AnnouncementService(IAnnouncementRepository announcementRepository) : IAnnouncementService
{
    public Task<IEnumerable<Announcement>> GetAllAnnouncements()
    {
        return announcementRepository.GetAll();
    }
    public IQueryable<Announcement> GetAllAnnouncementsQueryable(ApplicationDbContext context)
    {
        return announcementRepository.GetAllQueryable(context);
    }

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