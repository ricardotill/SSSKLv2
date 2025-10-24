using System.Globalization;
using System.Text;
using SSSKLv2.Components.Pages;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class OrderService(
    IOrderRepository orderRepository,
    IAchievementService achievementService,
    IPurchaseNotifier purchaseNotifier,
    IProductService productService,
    IApplicationUserService applicationUserService,
    ILogger<OrderService> logger) : IOrderService
{
    public Task<int> GetCount() => orderRepository.GetCount();
    public Task<int> GetPersonalCount(string username) => orderRepository.GetPersonalCount(username);
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext dbContext)
    {
        logger.LogInformation("{Type}: Get All Orders as Queryable", nameof(OrderService));
        return orderRepository.GetAllQueryable(dbContext);
    }
    
    public async Task<IList<Order>> GetAll(int skip, int take)
    {
        logger.LogInformation("{Type}: Get All Orders (paged) skip={Skip} take={Take}", nameof(OrderService), skip, take);
        return await orderRepository.GetAll(skip, take);
    }
    
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext dbContext)
    {
        logger.LogInformation("{Type}: Get Personal Orders as Queryable for user with username {Username}", nameof(OrderService), username);
        var output = orderRepository.GetPersonalQueryable(username, dbContext);
        return output;
    }
    
    public async Task<IList<Order>> GetPersonal(string username, int skip, int take)
    {
        logger.LogInformation("{Type}: Get Personal Orders (paged) for {Username} skip={Skip} take={Take}", nameof(OrderService), username, skip, take);
        return await orderRepository.GetPersonal(username, skip, take);
    }
    
    public async Task<IEnumerable<Order>> GetLatestOrders()
    {
        logger.LogInformation("{Type}: Get Latest Orders", nameof(OrderService));
        return await orderRepository.GetLatest();
    }
    
    public async Task<Order> GetOrderById(Guid id)
    {
        logger.LogInformation("{Type}: Get Order with ID {Id}", nameof(OrderService), id);
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

        logger.LogInformation("{Type}: Create order for {ProductsCount} products and {UsersCount} users", nameof(OrderService), products.Count, users.Count);

        foreach (var p in products)
        {
            var generated = GenerateUserOrders(users, p, order.Amount, order.Split);
            orders.AddRange(generated);
        }

        await orderRepository.CreateRange(orders);
        await NotifyPurchase(orders);
        await achievementService.CheckOrdersForAchievements(orders);
        foreach (var user in order.Users)
        {
            await achievementService.CheckUserForAchievements(user.Value.UserName!);
        }
    }

    // New overload: accept API DTO directly
    public async Task CreateOrder(OrderSubmitDto order)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));

        // Fetch products by ids
        var products = new List<Product>();
        foreach (var pid in order.Products)
        {
            var p = await productService.GetProductById(pid);
            products.Add(p);
        }

        // Fetch users by ids (ApplicationUser.Id is string, DTO uses GUIDs so convert)
        var users = new List<ApplicationUser>();
        foreach (var uid in order.Users)
        {
            var user = await applicationUserService.GetUserById(uid.ToString());
            users.Add(user);
        }

        var orders = new List<Order>();

        logger.LogInformation("{Type}: Create order for {ProductsCount} products and {UsersCount} users", nameof(OrderService), products.Count, users.Count);

        foreach (var p in products)
        {
            var generated = GenerateUserOrders(users, p, order.Amount, order.Split);
            orders.AddRange(generated);
        }

        await orderRepository.CreateRange(orders);
        await NotifyPurchase(orders);
        await achievementService.CheckOrdersForAchievements(orders);
        foreach (var u in users)
        {
            await achievementService.CheckUserForAchievements(u.UserName!);
        }
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
        logger.LogInformation("{Type}: Delete Order with ID {Id}", nameof(OrderService), id);
        await orderRepository.Delete(id);
    }

    private IList<Order> GenerateUserOrders(IList<ApplicationUser> userList, Product p, int amount, bool goingDutch)
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
    
    private async Task NotifyPurchase(IEnumerable<Order> orders)
    {
        var date = DateTime.Now;
        foreach (var order in orders)
        {
            await purchaseNotifier.NotifyUserPurchaseAsync(new UserPurchaseEvent(
                order.User.FullName,
                order.ProductNaam,
                order.Amount,
                date
            ));
        }
    }
}