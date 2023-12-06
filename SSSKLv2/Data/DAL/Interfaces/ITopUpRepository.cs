namespace SSSKLv2.Data.DAL.Interfaces;

public interface ITopUpRepository
{
    public Task<IQueryable<TopUp>> GetAllQueryable();
    public Task<IQueryable<TopUp>> GetPersonalQueryable(string username);
    public Task<TopUp> GetById(Guid id);
    public Task Create(TopUp topup);
    public Task Delete(Guid id);
}