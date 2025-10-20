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
            .Include(x => x.Image)
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
            .Include(x => x.Achievement.Image)
            .Where(x => x.Achievement.Id == achievementId)
            .OrderBy(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public IQueryable<AchievementEntry> GetAllEntriesQueryable(ApplicationDbContext context)
    {
        return context.AchievementEntry
            .OrderBy(x => x.CreatedOn);
    }
    
    public async Task<IList<AchievementEntry>> GetAllEntriesOfUser(string userId)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.AchievementEntry
            .Include(x => x.Achievement)
            .Include(x => x.Achievement.Image)
            .Include(x => x.User)
            .Where(x => x.User.Id == userId)
            .OrderBy(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public async Task<IList<AchievementEntry>> GetPersonalUnseenAchievementEntries(string username)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.AchievementEntry
            .Include(x => x.Achievement)
            .Include(x => x.Achievement.Image)
            .Include(x => x.User)
            .Where(x => x.User.UserName == username)
            .Where(x => !x.HasSeen)
            .OrderBy(x => x.CreatedOn)
            .ToListAsync();
    }

    public async Task<Achievement> GetById(Guid id)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        var achievement = await context.Achievement
            .Include(x => x.Image)
            .Where(x => x.Id == id)
            .SingleOrDefaultAsync();
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
            context.Entry(newEntry).Property("AchievementId").CurrentValue = entry.Achievement.Id;
            context.Entry(newEntry).Property("UserId").CurrentValue = entry.User.Id;
            
            entriesToAdd.Add(newEntry);
        }
        
        await context.AchievementEntry.AddRangeAsync(entriesToAdd);
        await context.SaveChangesAsync();
    }

    public async Task Update(Achievement achievement)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();

        // Load existing achievement including its image
        var existing = await context.Achievement
            .Include(a => a.Image)
            .FirstOrDefaultAsync(a => a.Id == achievement.Id);

        if (existing == null)
            throw new NotFoundException("Achievement not found");

        // Update scalar properties
        existing.Name = achievement.Name;
        existing.Description = achievement.Description;
        existing.AutoAchieve = achievement.AutoAchieve;
        existing.Action = achievement.Action;
        existing.ComparisonOperator = achievement.ComparisonOperator;
        existing.ComparisonValue = achievement.ComparisonValue;

        // Handle image replacement/removal
        if (achievement.Image == null)
        {
            // User wants to remove the image
            if (existing.Image != null)
            {
                context.AchievementImage.Remove(existing.Image);
            }
        }
        else
        {
            // New image provided. Remove old image if present, then insert new AchievementImage
            if (existing.Image != null)
            {
                context.AchievementImage.Remove(existing.Image);
                await context.SaveChangesAsync();
            }

            // Create a new AchievementImage entity and set the shadow FK AchievementId to link it
            var newImage = new AchievementImage
            {
                FileName = achievement.Image.FileName,
                Uri = achievement.Image.Uri,
                ContentType = achievement.Image.ContentType,
                CreatedOn = achievement.Image.CreatedOn == default ? DateTime.Now : achievement.Image.CreatedOn
            };

            // If the incoming image object has an Id (e.g. created by BlobStorageAgent), preserve it
            if (achievement.Image.Id != Guid.Empty)
            {
                newImage.Id = achievement.Image.Id;
            }

            await context.AchievementImage.AddAsync(newImage);
        }

        await context.SaveChangesAsync();
    }
    
    public async Task UpdateAchievementEntryRange(IEnumerable<AchievementEntry> entries)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        foreach (var entry in entries)
        {
            context.AchievementEntry.Update(entry);
        }
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
    
    public async Task DeleteAchievementEntryRange(IEnumerable<AchievementEntry> entries)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync();
        // Delete entries by their Ids to avoid issues with detached entities
        var ids = entries.Select(e => e.Id).ToList();
        var entriesInDb = await context.AchievementEntry.Where(e => ids.Contains(e.Id)).ToListAsync();
        if (entriesInDb.Any())
        {
            context.AchievementEntry.RemoveRange(entriesInDb);
            await context.SaveChangesAsync();
        }
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