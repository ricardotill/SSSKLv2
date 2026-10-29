using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;


namespace SSSKL.Application.Services;

public class AnnouncementService(IAnnouncementRepository announcementRepository) : IAnnouncementService
{
    public Task<int> GetCount() => announcementRepository.GetCount();
    public Task<IList<Announcement>> GetAllAnnouncements()
    {
        return announcementRepository.GetAll();
    }

    public async Task<IList<Announcement>> GetAllAnnouncements(int skip, int take)
    {
        return await announcementRepository.GetAllPaged(skip, take);
    }

    public Task<Announcement?> GetAnnouncementById(Guid id)
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