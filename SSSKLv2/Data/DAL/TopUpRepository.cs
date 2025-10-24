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

    public IQueryable<TopUp> GetAllQueryable(ApplicationDbContext context)
    {
        return context.TopUp
                .Include(x => x.User)
                .OrderByDescending(x => x.CreatedOn);
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
    
    public IQueryable<TopUp> GetPersonalQueryable(string username, ApplicationDbContext context)
    {
        return context.TopUp
            .Where(x => x.User.UserName == username)
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedOn);
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
        await using var context = await dbContextFactory.CreateDbContextAsync();
        topup.User.Saldo += topup.Saldo;
        context.Users.Update(topup.User);
        context.TopUp.Add(topup);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var entry = await context.TopUp
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (entry != null)
        {
            entry.User.Saldo -= entry.Saldo;
            context.Users.Update(entry.User);
            context.TopUp.Remove(entry);
            await context.SaveChangesAsync();
        }
        else throw new NotFoundException("TopUp Not Found");
    }
}