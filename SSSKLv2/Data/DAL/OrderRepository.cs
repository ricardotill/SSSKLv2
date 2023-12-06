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

    public Task<IEnumerable<Order>> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task CreateRange(IEnumerable<Order> orders)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var obj in orders)
        {
            obj.User.Saldo -= obj.Paid;    
            obj.User.LastOrdered = DateTime.UtcNow;
            context.Users.Update(obj.User);
            context.Order.Add(obj);
        }
        await context.SaveChangesAsync();
    }

    public async Task Create(Order obj)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        obj.User.Saldo -= obj.Paid;
        obj.User.LastOrdered = DateTime.UtcNow;
        context.Users.Update(obj.User);
        context.Order.Add(obj);
        await context.SaveChangesAsync();
    }

    public Task Update(Order obj)
    {
        throw new NotImplementedException();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entry = await context.Order
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (entry != null)
        {
            entry.User.Saldo += entry.Paid;
            context.Users.Update(entry.User);
            context.Order.Remove(entry);
            await context.SaveChangesAsync();
        }
        else throw new NotFoundException("Order Not Found");
    }
}