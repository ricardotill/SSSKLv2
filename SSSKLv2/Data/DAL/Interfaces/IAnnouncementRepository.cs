namespace SSSKLv2.Data.DAL.Interfaces;

public interface IAnnouncementRepository
{
    Task<int> GetCount();
    Task<IList<Announcement>> GetAll();
    Task<IList<Announcement>> GetAllPaged(int skip, int take);
    IQueryable<Announcement> GetAllQueryable(ApplicationDbContext context);
    Task<Announcement?> GetById(Guid id);
    Task Create(Announcement announcement);
    Task Update(Announcement announcement);
    Task Delete(Guid id);
}