using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Components.Account;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class TopUpRepository(
    IDbContextFactory<ApplicationDbContext> _dbContextFactory) : ITopUpRepository
{
    public async Task<IQueryable<TopUp>> GetAllQueryable()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return (await context.TopUp
                .Include(x => x.User)
                .ToListAsync())
                .AsQueryable();
    }
    
    public async Task<IQueryable<TopUp>> GetPersonalQueryable(string username)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return (await context.TopUp
                .Where(x => x.User.UserName == username)
                .Include(x => x.User)
                .ToListAsync())
                .AsQueryable();
    }
    
    public async Task<TopUp> GetById(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var topup = await context.TopUp
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id)!;
        if (topup != null)
        {
            return topup;
        }

        throw new NotFoundException("TopUp not found");
    }

    public async Task Create(TopUp topup)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        topup.User.Saldo += topup.Saldo;
        context.Users.Update(topup.User);
        context.TopUp.Add(topup);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entry = await context.TopUp
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id)!;
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