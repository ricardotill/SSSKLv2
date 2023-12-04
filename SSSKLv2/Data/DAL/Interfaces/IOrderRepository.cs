namespace SSSKLv2.Data.DAL.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    public Task<IQueryable<Order>> GetAllQueryable();
    public Task<IQueryable<Order>> GetPersonalQueryable(string username);
    public Task CreateRange(IEnumerable<Order> orders);
}
