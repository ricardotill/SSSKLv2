using SSSKLv2.Components.Pages;
using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IOrderService
{
    public Task<Order> GetOrderById(Guid id);
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext dbContext);
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext dbContext);
    public Task<IEnumerable<Order>> GetLatestOrders();
    public Task CreateOrder(Home.BestellingDto order);
    public Task DeleteOrder(Guid id);
}