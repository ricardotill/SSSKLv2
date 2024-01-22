using SSSKLv2.Components.Pages;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class OrderService(
    IOrderRepository _orderRepository,
    ILogger<OrderService> _logger) : IOrderService
{
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext dbContext)
    {
        _logger.LogInformation($"{GetType()}: Get All Orders as Queryable");
        return _orderRepository.GetAllQueryable(dbContext);
    }
    
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext dbContext)
    {
        _logger.LogInformation($"{GetType()}: Get Personal Orders as Queryable for user with username {username}");
        return _orderRepository.GetPersonalQueryable(username, dbContext);
    }
    
    public async Task<IEnumerable<Order>> GetLatestOrders()
    {
        _logger.LogInformation($"{GetType()}: Get Latest Orders");
        return await _orderRepository.GetLatest();
    }
    
    public async Task<Order> GetOrderById(Guid id)
    {
        _logger.LogInformation($"{GetType()}: Get Order with ID {id}");
        return await _orderRepository.GetById(id);
    }
    
    public async Task CreateOrder(Home.BestellingDto order)
    {
        var products = order.Products
            .Where(x => x.Selected)
            .Select(x => x.Value)
            .ToList();
        var users = order.Users
            .Where(x => x.Selected)
            .Select(x => x.Value)
            .ToList();
        var orders = new List<Order>();
        
        _logger.LogInformation($"{GetType()}: Create order for {products.Count} products and {products.Count} users");

        foreach (var p in products)
        {
            var generatedOrders = GenerateUserOrders(users, p, order.Amount, order.Split);
            orders.AddRange(generatedOrders);
        }
        
        await _orderRepository.CreateRange(orders);
    }

    public async Task DeleteOrder(Guid id)
    {
        _logger.LogInformation($"{GetType()}: Delete Order with ID {id}");
        await _orderRepository.Delete(id);
    }

    private IEnumerable<Order> GenerateUserOrders(IList<ApplicationUser> userList, Product p, int amount, bool goingDutch)
    {
        var paid = goingDutch ? Decimal.Round(p.Price / userList.Count, 2, MidpointRounding.ToPositiveInfinity) : p.Price;

        var list = new List<Order>();
        foreach (var u in userList)
        {
            var order = new Order()
            {
                User = u,
                Amount = amount,
                Paid = paid * amount,
                ProductNaam = p.Name,
                Product = p
            };
            list.Add(order);
        }

        return list;
    }
}