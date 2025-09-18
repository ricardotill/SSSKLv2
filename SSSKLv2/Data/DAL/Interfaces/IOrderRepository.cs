namespace SSSKLv2.Data.DAL.Interfaces;

public interface IOrderRepository
{
    public Task<IList<Order>> GetAllAsync();
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext context);
    public Task<IList<Order>> GetOrdersFromPastTwoYearsAsync();
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext context);
    public Task<IEnumerable<Order>> GetLatest();
    public Task CreateRange(IEnumerable<Order> orders);
    public Task<Order> GetById(Guid id);
    public Task Delete(Guid id);
}
