using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;


namespace SSSKL.Contracts.Services;

public interface IOrderService
{
    Task<int> GetCount();
    Task<int> GetPersonalCount(string username);
    public Task<Order> GetOrderById(Guid id);
    public Task<IList<Order>> GetAll(int skip, int take);
    public Task<IList<Order>> GetPersonal(string username, int skip, int take);
    public Task<IEnumerable<Order>> GetLatestOrders(int take = 10);
    public Task CreateOrder(OrderSubmitDto order, string? actingUserId);
    public Task<string> ExportAllOrdersToCsvAsync();
    public Task DeleteOrder(Guid id);
}
