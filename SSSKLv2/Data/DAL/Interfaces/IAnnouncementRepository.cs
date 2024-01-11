namespace SSSKLv2.Data.DAL.Interfaces;

public interface IAnnouncementRepository
{
    Task<IQueryable<Announcement>> GetAll();
    Task<IEnumerable<Announcement>> GetAllForEnduser();
    Task<Announcement> GetById(Guid id);
    Task Create(Announcement announcement);
    Task Update(Announcement announcement);
    Task Delete(Guid id);
}