using SSSKLv2.Components.Pages;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class OrderService(
    IOrderRepository _orderRepository) : IOrderService
{
    public async Task<IQueryable<Order>> GetAllQueryable()
    {
        return await _orderRepository.GetAllQueryable();
    }
    
    public async Task<IQueryable<Order>> GetPersonalQueryable(string username)
    {
        return await _orderRepository.GetPersonalQueryable(username);
    }
    
    public async Task<Order> GetOrderById(Guid id)
    {
        return await _orderRepository.GetById(id);
    }
    
    public async Task CreateOrder(Home.BestellingDTO dto)
    {
        var products = dto.Products
            .Where(x => x.Selected)
            .Select(x => x.Value)
            .ToList();
        var users = dto.Users
            .Where(x => x.Selected)
            .Select(x => x.Value)
            .ToList();
        var orders = new List<Order>();

        foreach (var p in products)
        {
            var generatedOrders = GenerateUserOrders(users, p, dto.Amount, dto.Split);
            orders.AddRange(generatedOrders);
        }
        
        await _orderRepository.CreateRange(orders);
    }

    public async Task DeleteOrder(Guid id)
    {
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
                ProductNaam = p.Name
            };
            list.Add(order);
        }

        return list;
    }
}