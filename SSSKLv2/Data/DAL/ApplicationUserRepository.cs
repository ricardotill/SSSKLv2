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
    
    public async Task<IList<ApplicationUser>> GetAllBySearchparam(string searchparam, int page)
    {
        IList<ApplicationUser> list = new List<ApplicationUser>() { };
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        list = await context.Users
            // Use AsNoTracking to disable EF change tracking
            .AsNoTracking()
            .Where(x => x.UserName.ToLower().Contains(searchparam)
                        || x.Email.ToLower().Contains(searchparam)
                        || x.Name.ToLower().Contains(searchparam)
                        || x.Surname.ToLower().Contains(searchparam))
            .OrderByDescending(x => x.Surname)
            .Skip(page * 5)
            .Take(5).ToListAsync();

        return list;
    }

    public async Task<IList<ApplicationUser>> GetAll()
    {
        IList<ApplicationUser> list = new List<ApplicationUser>();
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        list = await context.Users
            .OrderByDescending(e => e.LastOrdered)
            .ToListAsync();

        return list;
    }
}