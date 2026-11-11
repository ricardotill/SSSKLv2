using Microsoft.EntityFrameworkCore;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Infrastructure.Persistence.Repositories;

public class OrderRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IOrderRepository
{
    public async Task<int> GetCount()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Order.CountAsync();
    }
    
    public async Task<int> GetPersonalCount(string username)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Order
            .Where(x => x.User.UserName == username)
            .CountAsync();
    }

    public async Task<IList<Order>> GetAll(int skip, int take)
    {
        // Ensure sensible defaults / bounds
        if (skip < 0) skip = 0;
        if (take <= 0) take = 15;

        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Order
            .Include(x => x.User)
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<IList<Order>> GetAllAsync()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Order
            .Include(x => x.User)
            .Include(x => x.Product)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();
    }
    
    private static IQueryable<Order> GetPersonalQueryable(string username, ApplicationDbContext context)
    {
        return context.Order
            .Include(x => x.User)
            .Where(x => x.User.UserName == username)
            .OrderByDescending(x => x.CreatedOn);
    }

    public async Task<IList<Order>> GetPersonal(string username, int skip, int take)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) take = 15;

        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Order
            .Include(x => x.User)
            .Include(x => x.Product)
            .Where(x => x.User.UserName == username)
            .OrderByDescending(x => x.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<IList<Order>> GetPersonal(string username)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await GetPersonalQueryable(username, context).ToListAsync();
    }
    
    public async Task<IEnumerable<Order>> GetLatest(int take = 10)
    {
        var time = DateTime.Now.AddHours(-12);

        await using var context = await dbContextFactory.CreateDbContextAsync();
        return (await context.Order
                .Where(x => x.CreatedOn > time)
                .Include(x => x.User)
                .Include(x => x.Product)
                .OrderByDescending(x => x.CreatedOn)
                .Take(take)
                .ToListAsync());
    }
    
    public async Task<Order> GetById(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var order = await context.Order
            .Include(x => x.User)
            .Include(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (order != null)
        {
            return order;
        }

        throw new NotFoundException("Order not found");
    }
    

    public async Task CreateRange(IEnumerable<Order> orders)
    {
        var orderList = orders.ToList();
        if (orderList.Count == 0) return;

        var now = DateTime.Now;
        var saldoDeltas = orderList
            .GroupBy(o => o.User.Id)
            .Select(g => (UserId: g.Key, Delta: g.Sum(o => o.Paid)))
            .ToList();
        var stockDeltas = orderList
            .Where(o => o.Product != null)
            .GroupBy(o => o.Product!.Id)
            .Select(g => (ProductId: g.Key, Delta: g.Sum(o => o.Amount)))
            .ToList();

        await using var strategyContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            // Saldo and stock are adjusted with atomic UPDATE ... SET x = x - delta statements so
            // concurrent orders for the same user or product can't overwrite each other.
            foreach (var (userId, delta) in saldoDeltas)
            {
                await context.Users
                    .Where(u => u.Id == userId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.Saldo, u => u.Saldo - delta)
                        .SetProperty(u => u.LastOrdered, now));
            }
            foreach (var (productId, delta) in stockDeltas)
            {
                await context.Product
                    .Where(p => p.Id == productId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - delta));
            }

            AttachExisting(context, orderList);
            context.Order.AddRange(orderList);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        // Keep the in-memory entities in line with what was written
        foreach (var o in orderList)
        {
            o.User.Saldo -= o.Paid;
            o.User.LastOrdered = now;
            if (o.Product != null) o.Product.Stock -= o.Amount;
        }
    }

    public async Task<IList<OrderAggregate>> GetAmountAggregates(Guid productId, DateTime from, DateTime? to = null, IEnumerable<string>? userIds = null)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        var query = context.Order
            .Where(o => o.Product != null && o.Product.Id == productId && o.CreatedOn >= from);

        if (to.HasValue) query = query.Where(o => o.CreatedOn < to.Value);
        if (userIds != null)
        {
            var idSet = userIds.ToList();
            query = query.Where(o => idSet.Contains(o.User.Id));
        }

        return await query
            .GroupBy(o => o.User.Id)
            .Select(g => new OrderAggregate(g.Key, g.Sum(o => (long)o.Amount)))
            .ToListAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var strategyContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            var entry = await context.Order
                .Include(x => x.User)
                .SingleOrDefaultAsync(x => x.Id == id);
            if (entry == null) throw new NotFoundException("Order Not Found");

            await context.Users
                .Where(u => u.Id == entry.User.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Saldo, u => u.Saldo + entry.Paid));
            await context.Product
                .Where(p => p.Name == entry.ProductNaam)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + entry.Amount));

            context.Order.Remove(entry);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }

    // Orders reference users and products loaded by another context; attach them as unchanged so
    // EF neither inserts them nor overwrites their columns with stale values.
    private static void AttachExisting(ApplicationDbContext context, IEnumerable<Order> orders)
    {
        foreach (var order in orders)
        {
            if (context.Entry(order.User).State == EntityState.Detached) context.Attach(order.User);
            if (order.Product != null && context.Entry(order.Product).State == EntityState.Detached)
                context.Attach(order.Product);
        }
    }
}
