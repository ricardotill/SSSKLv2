using Microsoft.EntityFrameworkCore;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Infrastructure.Persistence.Repositories;

public class ReactionRepository(ApplicationDbContext dbContext) : IReactionRepository
{
    public async Task<Reaction?> GetById(Guid id)
    {
        return await dbContext.Reaction.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Reaction?> Find(Guid targetId, ReactionTargetType targetType, string userId, string content)
    {
        return await dbContext.Reaction
            .FirstOrDefaultAsync(r => r.TargetId == targetId &&
                                      r.TargetType == targetType &&
                                      r.UserId == userId &&
                                      r.Content == content);
    }

    public async Task<IList<Reaction>> GetByTargets(ReactionTargetType targetType, IEnumerable<Guid> targetIds)
    {
        var ids = targetIds.ToList();
        return await dbContext.Reaction
            .Include(r => r.User.ProfileImage)
            .Where(r => r.TargetType == targetType && ids.Contains(r.TargetId))
            .OrderBy(r => r.CreatedOn)
            .ToListAsync();
    }

    public async Task<IList<Reaction>> GetTimeline(int skip, int take)
    {
        return await dbContext.Reaction
            .Include(r => r.User.ProfileImage)
            .OrderByDescending(r => r.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountByTarget(ReactionTargetType targetType, Guid targetId)
    {
        return await dbContext.Reaction.CountAsync(r => r.TargetType == targetType && r.TargetId == targetId);
    }

    public async Task<IDictionary<Guid, int>> CountByTargets(ReactionTargetType targetType, IEnumerable<Guid> targetIds)
    {
        var ids = targetIds.ToList();
        return await dbContext.Reaction
            .Where(r => r.TargetType == targetType && ids.Contains(r.TargetId))
            .GroupBy(r => r.TargetId)
            .Select(g => new { TargetId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TargetId, x => x.Count);
    }

    public async Task Add(Reaction reaction)
    {
        dbContext.Reaction.Add(reaction);
        await dbContext.SaveChangesAsync();
    }

    public async Task Delete(Reaction reaction)
    {
        dbContext.Reaction.Remove(reaction);
        await dbContext.SaveChangesAsync();
    }

    public async Task<ISet<string>> DeleteWithReplies(Guid id)
    {
        var affectedUserIds = new HashSet<string>();
        await RemoveWithRepliesInternal(id, affectedUserIds);
        await dbContext.SaveChangesAsync();
        return affectedUserIds;
    }

    private async Task RemoveWithRepliesInternal(Guid id, ISet<string> affectedUserIds)
    {
        // Recursive deletion of all nested reactions
        var children = await dbContext.Reaction
            .Where(r => r.TargetId == id && r.TargetType == ReactionTargetType.Reaction)
            .Select(r => r.Id)
            .ToListAsync();

        foreach (var childId in children)
        {
            await RemoveWithRepliesInternal(childId, affectedUserIds);
        }

        var reaction = await dbContext.Reaction.FirstOrDefaultAsync(r => r.Id == id);
        if (reaction != null)
        {
            affectedUserIds.Add(reaction.UserId);
            dbContext.Reaction.Remove(reaction);
        }
    }
}
