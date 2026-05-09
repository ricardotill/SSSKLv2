using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class UserStatRepository(ApplicationDbContext context) : IUserStatRepository
{
    public async Task<UserStat> GetByUserId(string userId)
    {
        return await context.UserStats.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<UserStat> GetOrCreateByUserId(string userId)
    {
        var stats = await GetByUserId(userId);
        if (stats == null)
        {
            stats = new UserStat { UserId = userId };
            context.UserStats.Add(stats);
            await context.SaveChangesAsync();
        }
        return stats;
    }

    public async Task Update(UserStat userStat)
    {
        context.UserStats.Update(userStat);
        await context.SaveChangesAsync();
    }
}
