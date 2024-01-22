namespace SSSKLv2.Data.DAL.Interfaces;

public interface IOrderRepository
{
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext context);
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext context);
    public Task<IEnumerable<Order>> GetLatest();
    public Task CreateRange(IEnumerable<Order> orders);
    public Task<Order> GetById(Guid id);
    public Task Delete(Guid id);
}
