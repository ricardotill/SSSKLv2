using SSSKL.Domain.Entities;

namespace SSSKL.Contracts.Repositories;

public interface ITopUpRepository
{
    Task<int> GetCount();
    Task<int> GetPersonalCount(string username);
    Task<IList<TopUp>> GetAll();
    Task<IList<TopUp>> GetAll(int take, int skip);
    Task<IList<TopUp>> GetPersonal(string username);
    Task<IList<TopUp>> GetPersonal(string username, int take, int skip);
    Task<TopUp> GetById(Guid id);
    Task Create(TopUp topup);
    Task Delete(Guid id);
}