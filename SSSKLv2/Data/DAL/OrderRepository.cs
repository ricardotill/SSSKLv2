using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class OrderRepository(IDbContextFactory<ApplicationDbContext> _dbContextFactory) : IOrderRepository
{
    public async Task<PaginationObject<Order>> GetAllPagination(int page)
    {
        page -= 1;
        
        var pagination = new PaginationObject<Order>();
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        pagination.TotalObjects = await context.Order.CountAsync();
        pagination.Value = await context.Order
            // Use AsNoTracking to disable EF change tracking
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedOn)
            .Skip(page * 5)
            .Take(5).ToListAsync();

        return pagination;
    }
    
    public async Task<Order> GetById(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var order = await context.Order.FindAsync(id);
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

    public async Task Create(Order obj)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        obj.User.Saldo -= obj.Paid;
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
        var entry = await context.Order.FindAsync(id);
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