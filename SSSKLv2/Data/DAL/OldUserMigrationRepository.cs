using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class OldUserMigrationRepository(IDbContextFactory<ApplicationDbContext> _dbContextFactory) : IOldUserMigrationRepository
{
    public async Task<OldUserMigration> GetById(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entry = await context.OldUserMigration.FindAsync(id);
        if (entry != null)
        {
            return entry;
        }

        throw new NotFoundException("OldUserMigration not found");
    }
    
    public async Task<OldUserMigration> GetByUsername(string username)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entry = await context.OldUserMigration
            .SingleOrDefaultAsync(x =>
                string.Equals(x.Username.ToLower(), username.ToLower(), StringComparison.Ordinal));
        if (entry != null)
        {
            return entry;
        }

        throw new NotFoundException("OldUserMigration not found");
    }

    public async Task<IEnumerable<OldUserMigration>> GetAll()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var list = await context.OldUserMigration.ToListAsync();
        return list;
    }

    public async Task Create(OldUserMigration obj)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.OldUserMigration.Add(obj);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var entry = await context.OldUserMigration.FindAsync(id);
        if (entry != null)
        {
            context.OldUserMigration.Remove(entry);
            await context.SaveChangesAsync();
        }
        else throw new NotFoundException("OldUserMigration Not Found");
    }
}