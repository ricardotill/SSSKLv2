using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class OrderRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IOrderRepository
{
    public async Task<IList<Order>> GetAllAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Order
            .Include(x => x.User)
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public IQueryable<Order> GetAllQueryable(ApplicationDbContext context)
    {
        return context.Order
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedOn);
    }
    
    public async Task<IList<Order>> GetOrdersFromPastTwoYearsAsync()
    {
        var cutoff = DateTime.Now.AddYears(-2);
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Order
            .Where(x => x.CreatedOn >= cutoff)
            .Include(x => x.User)
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext context)
    {
        return context.Order
            .Include(x => x.User)
            .Where(x => x.User.UserName == username)
            .OrderByDescending(x => x.CreatedOn);
    }
    
    public async Task<IList<Order>> GetPersonal(string username)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await GetPersonalQueryable(username, context).ToListAsync();
    }
    
    public async Task<IEnumerable<Order>> GetLatest()
    {
        var time = DateTime.Now.AddHours(-12);

        await using var context = await dbContextFactory.CreateDbContextAsync();
        return (await context.Order
                .Where(x => x.CreatedOn > time)
                .Include(x => x.User)
                .OrderByDescending(x => x.CreatedOn)
                .Take(10)
                .ToListAsync());
    }
    
    public async Task<Order> GetById(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var order = await context.Order
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (order != null)
        {
            return order;
        }

        throw new NotFoundException("Order not found");
    }
    

    public async Task CreateRange(IEnumerable<Order> orders)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        foreach (var obj in orders)
        {
            UpdateUserSaldo(obj, context);
            if (obj.Product != null) UpdateProductInventory(obj, obj.Product, context);
            context.Order.Add(obj);
        }
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var entry = await context.Order
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == id);
        
        if (entry != null)
        {
            UpdateUserSaldo(entry, context, false);
            var product = await context.Product
                .SingleOrDefaultAsync(x => x.Name == entry.ProductNaam);
            if (product != null) UpdateProductInventory(entry, product, context, false);
            context.Order.Remove(entry);
            await context.SaveChangesAsync();
        }
        else throw new NotFoundException("Order Not Found");
    }

    private static void UpdateUserSaldo(
        Order order, 
        ApplicationDbContext context, 
        bool isNegative = true)
    {
        var money = order.Paid;
        if (isNegative) money = Decimal.Negate(money);
        order.User.Saldo += money;
        order.User.LastOrdered = DateTime.Now;
        context.Users.Update(order.User);
    }
    
    private static void UpdateProductInventory(
        Order order, 
        Product product, 
        ApplicationDbContext context, 
        bool isNegative = true)
    {
        var inventory = order.Amount;
        if (isNegative) inventory *= -1;
        product.Stock += inventory;
        context.Product.Update(product);
    }

}