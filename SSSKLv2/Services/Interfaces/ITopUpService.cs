using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface ITopUpService
{
    Task<int> GetCount();
    Task<int> GetPersonalCount(string username);
    Task<IEnumerable<TopUp>> GetAll();
    Task<IEnumerable<TopUp>> GetAll(int skip, int take);
    IQueryable<TopUp> GetAllQueryable(ApplicationDbContext dbContext);
    Task<IEnumerable<TopUp>> GetAllPersonal(string username);
    Task<IEnumerable<TopUp>> GetAllPersonal(string username, int skip, int take);
    IQueryable<TopUp> GetPersonalQueryable(string username, ApplicationDbContext dbContext);
    Task<TopUp> GetById(string id);
    Task CreateTopUp(TopUp topup);
    Task DeleteTopUp(Guid id);
}