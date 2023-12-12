using SSSKLv2.Components.Pages;
using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IOrderService
{
    public Task<Order> GetOrderById(Guid id);
    public Task<IQueryable<Order>> GetAllQueryable();
    public Task<IQueryable<Order>> GetPersonalQueryable(string username);
    public Task<IEnumerable<Order>> GetLatestOrders();
    public Task CreateOrder(Home.BestellingDto order);
    public Task DeleteOrder(Guid id);
}