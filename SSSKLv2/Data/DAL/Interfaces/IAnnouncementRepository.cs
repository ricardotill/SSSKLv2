namespace SSSKLv2.Data.DAL.Interfaces;

public interface IAnnouncementRepository
{
    Task<IEnumerable<Announcement>> GetAll();
    IQueryable<Announcement> GetAllQueryable(ApplicationDbContext context);
    // Task<IEnumerable<Announcement>> GetAllForEnduser();
    Task<Announcement> GetById(Guid id);
    Task Create(Announcement announcement);
    Task Update(Announcement announcement);
    Task Delete(Guid id);
}