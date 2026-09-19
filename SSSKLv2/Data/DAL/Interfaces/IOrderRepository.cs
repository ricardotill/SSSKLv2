namespace SSSKLv2.Data.DAL.Interfaces;

public interface IOrderRepository
{
    Task<int> GetCount();
    Task<int> GetPersonalCount(string username);
    public Task<IList<Order>> GetAllAsync();
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext context);
    public Task<IList<Order>> GetAll(int skip, int take);
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext context);
    public Task<IList<Order>> GetPersonal(string username, int skip, int take);
    public Task<IList<Order>> GetPersonal(string username);
    public Task<IEnumerable<Order>> GetLatest(int take = 10);
    public Task CreateRange(IEnumerable<Order> orders);
    public Task<Order> GetById(Guid id);
    public Task Delete(Guid id);
    // SQL-side GROUP BY sum per user, optionally restricted to a set of user ids and/or an end date
    public Task<IList<OrderAggregate>> GetAmountAggregates(Guid productId, DateTime from, DateTime? to = null, IEnumerable<string>? userIds = null);
}
