namespace SSSKLv2.Data.DAL.Interfaces;

public interface IOrderRepository
{
    public Task<IQueryable<Order>> GetAllQueryable();
    public Task<IQueryable<Order>> GetPersonalQueryable(string username);
    public Task CreateRange(IEnumerable<Order> orders);
    public Task<Order> GetById(Guid id);
    public Task Delete(Guid id);
}
