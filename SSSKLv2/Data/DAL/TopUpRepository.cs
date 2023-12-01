using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Components.Account;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class TopUpRepository(
    IDbContextFactory<ApplicationDbContext> _dbContextFactory,
    UserManager<ApplicationUser> _userManager ) : ITopUpRepository
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
    
    public async Task<PaginationObject<TopUp>> GetPersonalPagination(int page, string id)
    {
        page -= 1;
        
        var pagination = new PaginationObject<TopUp>();
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        pagination.TotalObjects = await context.TopUp.CountAsync();
        pagination.Value = await context.TopUp
            // Use AsNoTracking to disable EF change tracking
            .AsNoTracking()
            .Where(x => x.User.Id == id)
            .Include(e => e.User)
            .OrderByDescending(x => x.CreatedOn)
            .Skip(page * 5)
            .Take(5).ToListAsync();

        return pagination;
    }
    
    public async Task<TopUp> GetById(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var topup = await context.TopUp.FindAsync(id);
        if (topup != null)
        {
            return topup;
        }

        throw new NotFoundException("TopUp not found");
    }

    public Task<IEnumerable<TopUp>> GetAll()
    {
        throw new NotImplementedException();
    }

    public async Task Create(TopUp obj)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        obj.User.Saldo += obj.Saldo;
        context.Users.Update(obj.User);
        context.TopUp.Add(obj);
        await context.SaveChangesAsync();
        // await UpdateSaldoClaim(obj.User);
    }

    public Task Update(TopUp obj)
    {
        throw new NotImplementedException();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entry = await context.TopUp
            .FindAsync(id);
        if (entry != null)
        {
            entry.User.Saldo -= entry.Saldo;
            context.Users.Update(entry.User);
            context.TopUp.Remove(entry);
            await context.SaveChangesAsync();
            await UpdateSaldoClaim(entry.User);
        }
        else throw new NotFoundException("TopUp Not Found");
    }

    private async Task UpdateSaldoClaim(ApplicationUser user)
    {
        var oldClaims = await _userManager.GetClaimsAsync(user);
        var oldClaim = oldClaims.FirstOrDefault(e => e.Type == IdentityClaim.Saldo.ToString());
        if (oldClaim != null)
        {
            await _userManager.RemoveClaimAsync(user, oldClaim);
        }
        await _userManager.AddClaimAsync(user, new Claim(IdentityClaim.Saldo.ToString(), user.Saldo.ToString("C")));
    }
}