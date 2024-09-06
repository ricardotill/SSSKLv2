using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class AchievementRepository(IDbContextFactory<ApplicationDbContext> _dbContextFactory) : IAchievementRepository
{
    public async Task<IEnumerable<Achievement>> GetAll()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Achievement
            .OrderBy(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<AchievementEntry>> GetAllEntriesOfAchievement(Guid achievementId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.AchievementEntry
            .Where(x => x.Achievement.Id == achievementId)
            .OrderBy(x => x.CreatedOn)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<AchievementEntry>> GetAllEntriesOfUser(string userId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.AchievementEntry
            .Where(x => x.User.Id == userId)
            .OrderBy(x => x.CreatedOn)
            .ToListAsync();
    }

    public async Task<Achievement> GetById(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var achievement = await context.Achievement.FindAsync(id);
        if (achievement != null) return achievement;
        
        throw new NotFoundException("Achievement not found");
    }

    public async Task Create(Achievement achievement)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.AddAsync(achievement);
        await context.SaveChangesAsync();
    }

    public async Task Update(Achievement achievement)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Update(achievement);
        await context.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var achievement = await context.Achievement.FindAsync(id);
        if (achievement != null)
        {
            context.Remove(achievement);
            await context.SaveChangesAsync();
        }
        else throw new NotFoundException("Achievement not found");
    }
}