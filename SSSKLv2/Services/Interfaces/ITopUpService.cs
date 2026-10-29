using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface ITopUpService
{
    Task<int> GetCount();
    Task<int> GetPersonalCount(string username);
    Task<IEnumerable<TopUp>> GetAll();
    Task<IEnumerable<TopUp>> GetAll(int skip, int take);
    Task<IEnumerable<TopUp>> GetAllPersonal(string username);
    Task<IEnumerable<TopUp>> GetAllPersonal(string username, int skip, int take);
    Task<TopUp> GetById(string id);
    Task CreateTopUp(TopUp topup);
    Task DeleteTopUp(Guid id);
}