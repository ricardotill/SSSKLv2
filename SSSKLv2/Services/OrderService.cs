using System.Globalization;
using System.Text;
using SSSKLv2.Components.Pages;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class OrderService(
    IOrderRepository orderRepository,
    ILogger<OrderService> logger) : IOrderService
{
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext dbContext)
    {
        logger.LogInformation("{Type}: Get All Orders as Queryable", GetType());
        return orderRepository.GetAllQueryable(dbContext);
    }
    
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext dbContext)
    {
        logger.LogInformation("{Type}: Get Personal Orders as Queryable for user with username {Username}", GetType(), username);
        var output = orderRepository.GetPersonalQueryable(username, dbContext);
        return output;
    }
    
    public async Task<IEnumerable<Order>> GetLatestOrders()
    {
        logger.LogInformation("{Type}: Get Latest Orders", GetType());
        return await orderRepository.GetLatest();
    }
    
    public async Task<Order> GetOrderById(Guid id)
    {
        logger.LogInformation("{Type}: Get Order with ID {Id}", GetType(), id);
        return await orderRepository.GetById(id);
    }
    
    public async Task CreateOrder(POS.BestellingDto order)
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
        
        logger.LogInformation("{Type}: Create order for {productsCount} products and {usersCount} users", GetType(), products.Count, users.Count);

        foreach (var p in products)
        {
            var generatedOrders = GenerateUserOrders(users, p, order.Amount, order.Split);
            orders.AddRange(generatedOrders);
        }
        
        await orderRepository.CreateRange(orders);
    }
    
    public async Task<string> ExportOrdersFromPastTwoYearsToCsvAsync()
    {
        var orders = await orderRepository.GetOrdersFromPastTwoYearsAsync(); // Returns IEnumerable<Order>
        var csv = new StringBuilder();

        // Header
        csv.AppendLine("OrderId,CustomerUsername,OrderDateTime,ProductName,ProductAmount,TotalPaid");

        // Rows
        foreach (var order in orders)
        {
            csv.AppendLine(
                $"{order.Id},{EscapeCsvField(order.User != null ? order.User.UserName : null)},{order.CreatedOn:yyyy-MM-dd HH:mm:ss},{EscapeCsvField(order.ProductNaam)},{order.Amount},{order.Paid.ToString(CultureInfo.InvariantCulture)}");
        }

        return csv.ToString();
    }

    // Escapes a field for CSV output, including formula injection mitigation
    private static string EscapeCsvField(string field)
    {
        if (field == null)
            return "\"\"";
        // Formula injection mitigation: prefix with ' if starts with =, +, -, or @
        if (field.StartsWith('=') || field.StartsWith('+') || field.StartsWith('-') || field.StartsWith('@'))
            field = "'" + field;
        // Escape double quotes by doubling them
        field = field.Replace("\"", "\"\"");
        // Wrap in double quotes
        return $"\"{field}\"";
    }
    public async Task DeleteOrder(Guid id)
    {
        logger.LogInformation("{Type}: Delete Order with ID {Id}", GetType(), id);
        await orderRepository.Delete(id);
    }

    private IEnumerable<Order> GenerateUserOrders(IList<ApplicationUser> userList, Product p, int amount, bool goingDutch)
    {
        var paid = goingDutch ? Decimal.Round(p.Price / userList.Count, 2, MidpointRounding.ToPositiveInfinity) : p.Price;
        var sharedAmount = goingDutch ? amount / userList.Count : amount;
        
        var list = new List<Order>();
        foreach (var u in userList)
        {
            var order = new Order()
            {
                User = u,
                Amount = sharedAmount,
                Paid = paid * amount,
                ProductNaam = p.Name,
                Product = p
            };
            list.Add(order);
        }

        return list;
    }
}