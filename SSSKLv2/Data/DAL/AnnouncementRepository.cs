using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class AnnouncementRepository(IDbContextFactory<ApplicationDbContext> _dbContextFactory) : IAnnouncementRepository
{
    public async Task<IEnumerable<Announcement>> GetAll()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Announcement
            .OrderBy(x => x.Order)
            .ThenBy(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public IQueryable<Announcement> GetAllQueryable(ApplicationDbContext context)
    {
        return context.Announcement
            .OrderBy(x => x.Order)
            .ThenBy(x => x.CreatedOn);
    }
    
    // public async Task<IEnumerable<Announcement>> GetAllForEnduser()
    // {
    //     await using var context = await _dbContextFactory.CreateDbContextAsync();
    //     
    //     return await context.Announcement
    //         .Where(x => !x.IsScheduled 
    //         || (x.IsScheduled && 
    //             x.PlannedFrom!.Value.CompareTo(DateTime.Today) <= 0 && 
    //             x.PlannedTill!.Value.CompareTo(DateTime.Today) >= 0))
    //         .OrderBy(x => x.Order)
    //         .ThenBy(x => x.CreatedOn)
    //         .ToListAsync();
    // }

    public async Task<Announcement> GetById(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var announcement = await context.Announcement.FindAsync(id);
        if (announcement != null) return announcement;
        
        throw new NotFoundException("Announcement not found");
    }

    public async Task Create(Announcement announcement)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(announcement);
        await context.SaveChangesAsync();
    }

    public async Task Update(Announcement announcement)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Update(announcement);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var announcement = await context.Announcement.FindAsync(id);
        if (announcement != null)
        {
            context.Remove(announcement);
            await context.SaveChangesAsync();
        }
        else throw new NotFoundException("Announcement not found");
    }
}