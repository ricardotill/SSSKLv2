namespace SSSKLv2.Data.DAL.Interfaces;

public interface ITopUpRepository
{
    Task<int> GetCount();
    Task<int> GetPersonalCount(string username);
    Task<IList<TopUp>> GetAll();
    Task<IList<TopUp>> GetAll(int take, int skip);
    IQueryable<TopUp> GetAllQueryable(ApplicationDbContext context);
    Task<IList<TopUp>> GetPersonal(string username);
    Task<IList<TopUp>> GetPersonal(string username, int take, int skip);
    IQueryable<TopUp> GetPersonalQueryable(string username, ApplicationDbContext context);
    Task<TopUp> GetById(Guid id);
    Task Create(TopUp topup);
    Task Delete(Guid id);
}