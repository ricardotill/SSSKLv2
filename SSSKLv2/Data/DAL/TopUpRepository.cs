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
    public async Task<PaginationObject<TopUp>> GetAllPagination(int page)
    {
        page -= 1;
        
        var pagination = new PaginationObject<TopUp>();
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        pagination.TotalObjects = await context.TopUp.CountAsync();
        pagination.Value = await context.TopUp
            // Use AsNoTracking to disable EF change tracking
            .AsNoTracking()
            .Include(e => e.User)
            .OrderByDescending(x => x.CreatedOn)
            .Skip(page * 5)
            .Take(5).ToListAsync();

        return pagination;
    }
    
    public async Task<PaginationObject<TopUp>> GetPersonalPagination(int page, string username)
    {
        page -= 1;
        
        var pagination = new PaginationObject<TopUp>();
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        pagination.TotalObjects = await context.TopUp.CountAsync();
        pagination.Value = await context.TopUp
            // Use AsNoTracking to disable EF change tracking
            .AsNoTracking()
            .Where(x => x.User.UserName == username)
            .Include(e => e.User)
            .OrderByDescending(x => x.CreatedOn)
            .Skip(page * 5)
            .Take(5).ToListAsync();

        return pagination;
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

    public async Task Create(TopUp obj)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        obj.User.Saldo += obj.Saldo;
        context.Users.Update(obj.User);
        context.TopUp.Add(obj);
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