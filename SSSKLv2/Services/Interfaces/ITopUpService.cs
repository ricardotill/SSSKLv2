using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface ITopUpService
{
    public Task<IQueryable<TopUp>> GetAllQueryable();
    public Task<IQueryable<TopUp>> GetPersonalQueryable(string username);
    public Task<TopUp> GetById(string id);
    public Task CreateTopUp(TopUp topup);
    public Task DeleteTopUp(Guid id);
}