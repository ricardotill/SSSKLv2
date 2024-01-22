using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface ITopUpService
{
    public IQueryable<TopUp> GetAllQueryable(ApplicationDbContext dbContext);
    public IQueryable<TopUp> GetPersonalQueryable(string username, ApplicationDbContext dbContext);
    public Task<TopUp> GetById(string id);
    public Task CreateTopUp(TopUp topup);
    public Task DeleteTopUp(Guid id);
}