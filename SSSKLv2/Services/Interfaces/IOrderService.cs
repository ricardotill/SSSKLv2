using SSSKLv2.Components.Pages;
using SSSKLv2.Data;
using SSSKLv2.Dto.Api.v1;

namespace SSSKLv2.Services.Interfaces;

public interface IOrderService
{
    public Task<Order> GetOrderById(Guid id);
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext dbContext);
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext dbContext);
    public Task<IEnumerable<Order>> GetLatestOrders();
    public Task CreateOrder(POS.BestellingDto order);
    public Task CreateOrder(OrderSubmitDto order);
    public Task<string> ExportOrdersFromPastTwoYearsToCsvAsync();
    public Task DeleteOrder(Guid id);
}