using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class AchievementRepository(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IAchievementRepository
{
    public async Task<IEnumerable<Achievement>> GetAll()
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Achievement
            .OrderBy(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public IQueryable<Achievement> GetAllQueryable(ApplicationDbContext context)
    {
        return context.Achievement
            .OrderBy(x => x.CreatedOn);
    }
    
    public async Task<IEnumerable<AchievementEntry>> GetAllEntries(Guid achievementId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.AchievementEntry
            .Where(x => x.Achievement.Id == achievementId)
            .OrderBy(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public IQueryable<AchievementEntry> GetAllEntriesQueryable(ApplicationDbContext context)
    {
        return context.AchievementEntry
            .OrderBy(x => x.CreatedOn);
    }
    
    public async Task<IEnumerable<AchievementEntry>> GetAllEntriesOfUser(string userId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.AchievementEntry
            .Where(x => x.User.Id == userId)
            .OrderBy(x => x.CreatedOn)
            .ToListAsync();
    }

    public async Task<Achievement> GetById(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var achievement = await context.Achievement.FindAsync(id);
        if (achievement != null) return achievement;
        
        throw new NotFoundException("Achievement not found");
    }

    public async Task Create(Achievement achievement)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(achievement);
        await context.SaveChangesAsync();
    }
    
    public async Task CreateEntryRange(IEnumerable<AchievementEntry> entries)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        
        var entriesToAdd = new List<AchievementEntry>();
        foreach (var entry in entries)
        {
            var newEntry = new AchievementEntry
            {
                HasSeen = entry.HasSeen,
                CreatedOn = entry.CreatedOn,
                Achievement = null!, // Don't set navigation property to avoid re-inserting Achievement
                User = null! // Don't set navigation property to avoid re-inserting User
            };
            
            // Set the foreign keys directly
            context.Entry(newEntry).Property("AchievementId").CurrentValue = entry.Achievement?.Id;
            context.Entry(newEntry).Property("UserId").CurrentValue = entry.User?.Id;
            
            entriesToAdd.Add(newEntry);
        }
        
        await context.AchievementEntry.AddRangeAsync(entriesToAdd);
        await context.SaveChangesAsync();
    }

    public async Task Update(Achievement achievement)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        context.Update(achievement);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var achievement = await context.Achievement.FindAsync(id);
        if (achievement != null)
        {
            context.Remove(achievement);
            await context.SaveChangesAsync();
        }
        else throw new NotFoundException("Achievement not found");
    }

    public async Task<IEnumerable<Achievement>> GetUncompletedAchievementsForUser(string userId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        
        // Get all achievement IDs that the user has completed
        var completedAchievementIds = context.AchievementEntry
            .Where(entry => entry.User.Id == userId)
            .Select(entry => entry.Achievement.Id);
        
        // Get all achievements that are NOT in the completed list
        return await context.Achievement
            .Where(achievement => !completedAchievementIds.Contains(achievement.Id))
            .OrderBy(achievement => achievement.CreatedOn)
            .ToListAsync();
    }
}