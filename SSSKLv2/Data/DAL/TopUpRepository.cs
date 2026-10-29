using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class TopUpRepository(
    IDbContextFactory<ApplicationDbContext> dbContextFactory) : ITopUpRepository
{
    public async Task<int> GetCount()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.TopUp.CountAsync();
    }
    
    public async Task<int> GetPersonalCount(string username)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.TopUp
            .Where(x => x.User.UserName == username)
            .CountAsync();
    }
    
    public async Task<IList<TopUp>> GetAll()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.TopUp
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public async Task<IList<TopUp>> GetAll(int take, int skip)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.TopUp
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<IList<TopUp>> GetPersonal(string username)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.TopUp
            .Where(x => x.User.UserName == username)
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public async Task<IList<TopUp>> GetPersonal(string username, int take, int skip)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.TopUp
            .Where(x => x.User.UserName == username)
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
    
    public async Task<TopUp> GetById(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var topup = await context.TopUp
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (topup != null)
        {
            return topup;
        }

        throw new NotFoundException("TopUp not found");
    }

    public async Task Create(TopUp topup)
    {
        await using var strategyContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            // Atomic increment so concurrent orders/top-ups for the same user can't overwrite each other
            await context.Users
                .Where(u => u.Id == topup.User.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Saldo, u => u.Saldo + topup.Saldo));

            if (context.Entry(topup.User).State == EntityState.Detached) context.Attach(topup.User);
            context.TopUp.Add(topup);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });

        topup.User.Saldo += topup.Saldo;
    }

    public async Task Delete(Guid id)
    {
        await using var strategyContext = await dbContextFactory.CreateDbContextAsync();
        var strategy = strategyContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            var entry = await context.TopUp
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entry == null) throw new NotFoundException("TopUp Not Found");

            await context.Users
                .Where(u => u.Id == entry.User.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Saldo, u => u.Saldo - entry.Saldo));

            context.TopUp.Remove(entry);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        });
    }
}
