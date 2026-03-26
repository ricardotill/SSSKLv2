using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class AnnouncementRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IAnnouncementRepository
{
    public async Task<int> GetCount()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Announcement.CountAsync();
    }

    public async Task<IList<Announcement>> GetAll()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
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

    public async Task<Announcement?> GetById(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var announcement = await context.Announcement.FindAsync(id);
        if (announcement != null) return announcement;
        
        throw new NotFoundException("Announcement not found");
    }

    public async Task<IList<Announcement>> GetAllPaged(int skip, int take)
    {
        // Ensure sensible bounds for skip/take
        if (skip < 0) skip = 0;
        if (take <= 0) take = 50; // default page size when invalid

        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Announcement
            .OrderBy(x => x.Order)
            .ThenBy(x => x.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task Create(Announcement announcement)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(announcement);
        await context.SaveChangesAsync();
    }

    public async Task Update(Announcement announcement)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Update(announcement);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var announcement = await context.Announcement.FindAsync(id);
        if (announcement != null)
        {
            context.Remove(announcement);
            await context.SaveChangesAsync();
        }
        else throw new NotFoundException("Announcement not found");
    }
}