namespace SSSKLv2.Data.DAL.Interfaces;

public interface IReactionRepository
{
    Task<Reaction?> GetById(Guid id);
    Task<Reaction?> Find(Guid targetId, ReactionTargetType targetType, string userId, string content);
    // Includes User and ProfileImage, ordered by CreatedOn
    Task<IList<Reaction>> GetByTargets(ReactionTargetType targetType, IEnumerable<Guid> targetIds);
    Task<IList<Reaction>> GetTimeline(int skip, int take);
    Task<int> CountByTarget(ReactionTargetType targetType, Guid targetId);
    Task<IDictionary<Guid, int>> CountByTargets(ReactionTargetType targetType, IEnumerable<Guid> targetIds);
    Task Add(Reaction reaction);
    Task Delete(Reaction reaction);
    // Deletes the reaction and all nested replies; returns the ids of the users whose reactions were removed
    Task<ISet<string>> DeleteWithReplies(Guid id);
}
