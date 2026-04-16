using SSSKLv2.Data;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Services.Interfaces;

public interface IOrderService
{
    Task<int> GetCount();
    Task<int> GetPersonalCount(string username);
    public Task<Order> GetOrderById(Guid id);
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext dbContext);
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext dbContext);
    public Task<IList<Order>> GetAll(int skip, int take);
    public Task<IList<Order>> GetPersonal(string username, int skip, int take);
    public Task<IEnumerable<Order>> GetLatestOrders(int take = 10);
    public Task CreateOrder(OrderSubmitDto order, string? actingUserId);
    public Task<string> ExportOrdersFromPastTwoYearsToCsvAsync();
    public Task DeleteOrder(Guid id);
}