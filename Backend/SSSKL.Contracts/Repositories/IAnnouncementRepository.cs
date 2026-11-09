using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Repositories;

public interface IAnnouncementRepository
{
    Task<int> GetCount();
    Task<IList<Announcement>> GetAll();
    Task<IList<Announcement>> GetAllPaged(int skip, int take);
    Task<Announcement?> GetById(Guid id);
    Task Create(Announcement announcement);
    Task Update(Announcement announcement);
    Task Delete(Guid id);
}