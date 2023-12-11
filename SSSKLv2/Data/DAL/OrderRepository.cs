using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class OrderRepository(IDbContextFactory<ApplicationDbContext> _dbContextFactory) : IOrderRepository
{
    public async Task<IQueryable<Order>> GetAllQueryable()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return (await context.Order
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync())
            .AsQueryable();
    }
    
    public async Task<IQueryable<Order>> GetPersonalQueryable(string username)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return (await context.Order
            .Include(x => x.User)
            .Where(x => x.User.UserName == username)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync())
            .AsQueryable();
    }
    
    public async Task<Order> GetById(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
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
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var obj in orders)
        {
            UpdateUserSaldo(obj, context);
            UpdateProductInventory(obj, obj.Product, context);
            context.Order.Add(obj);
        }
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
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

    private void UpdateUserSaldo(
        Order order, 
        ApplicationDbContext context, 
        bool isNegative = true)
    {
        var money = order.Paid;
        if (isNegative) money = Decimal.Negate(money);
        order.User.Saldo += money;
        order.User.LastOrdered = DateTime.UtcNow;
        context.Users.Update(order.User);
    }
    
    private void UpdateProductInventory(
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