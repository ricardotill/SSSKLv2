using SSSKL.Contracts.Dto;


namespace SSSKL.Contracts.Services;

public interface IReactionService
{
    Task ToggleReaction(Guid targetId, string targetType, string content, string userId);
    Task<IEnumerable<ReactionDto>> GetReactionsForTarget(Guid targetId, string targetType);
    Task<IEnumerable<ReactionDto>> GetTimeline(int skip, int take);
    Task DeleteReaction(Guid id, string userId, bool isAdmin);
}
