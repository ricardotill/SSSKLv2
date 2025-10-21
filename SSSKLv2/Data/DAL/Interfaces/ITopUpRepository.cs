namespace SSSKLv2.Data.DAL.Interfaces;

public interface ITopUpRepository
{
    public IQueryable<TopUp> GetAllQueryable(ApplicationDbContext context);
    public IQueryable<TopUp> GetPersonalQueryable(string username, ApplicationDbContext context);
    public Task<IList<TopUp>> GetPersonal(string username);
    public Task<TopUp> GetById(Guid id);
    public Task Create(TopUp topup);
    public Task Delete(Guid id);
}