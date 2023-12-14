using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class ApplicationUserRepository(IDbContextFactory<ApplicationDbContext> _dbContextFactory) : IApplicationUserRepository
{
    public async Task<ApplicationUser> GetById(string id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entry = await context.Users.FindAsync(id);
        if (entry != null)
        {
            return entry;
        }

        throw new NotFoundException("ApplicationUser not found");
    }
    
    public async Task<ApplicationUser> GetByUsername(string username)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entry = await context.Users
            .Where(x => x.UserName == username)
            .FirstOrDefaultAsync();
        if (entry != null)
        {
            return entry;
        }

        throw new NotFoundException("ApplicationUser not found");
    }
    
    public async Task<IList<ApplicationUser>> GetAllForAdmin()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var list = await context.Users
            .AsNoTracking()
            .OrderByDescending(x => x.Surname)
            .ToListAsync();

        return list;
    }

    public async Task<IList<ApplicationUser>> GetAll()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var list = await GetConsumerUsersQuery(context)
            .Where(s => s.Orders.Any())
            .OrderByDescending(e => e.LastOrdered)
            .ToListAsync();

        return list;
    }
    
    public async Task<IList<ApplicationUser>> GetAllWithOrders()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        var list = await GetConsumerUsersQuery(context)
            .Where(s => s.Orders.Any())
            .Include(x => x.Orders)
            .ThenInclude(x => x.Product)
            .OrderByDescending(e => e.LastOrdered)
            .ToListAsync();

        return list;
    }

    private IQueryable<ApplicationUser> GetConsumerUsersQuery(ApplicationDbContext context)
    {
        return from u in context.Users
            join ur in context.UserRoles on u.Id equals ur.UserId
            join r in context.Roles on ur.RoleId equals r.Id
            where r.Name != "Kiosk"
            select u;
    }
}