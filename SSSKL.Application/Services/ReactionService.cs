using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;


namespace SSSKL.Application.Services;

public class ReactionService : IReactionService
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IApplicationUserRepository _applicationUserRepository;
    private readonly IApplicationUserService _userService;
    private readonly INotificationService _notificationService;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IUserStatRepository _userStatRepository;

    public ReactionService(IReactionRepository reactionRepository, IEventRepository eventRepository, IApplicationUserRepository applicationUserRepository, IApplicationUserService userService, INotificationService notificationService, IDomainEventDispatcher eventDispatcher, IUserStatRepository userStatRepository)
    {
        _reactionRepository = reactionRepository;
        _eventRepository = eventRepository;
        _applicationUserRepository = applicationUserRepository;
        _userService = userService;
        _notificationService = notificationService;
        _eventDispatcher = eventDispatcher;
        _userStatRepository = userStatRepository;
    }

    public async Task ToggleReaction(Guid targetId, string targetTypeStr, string content, string userId)
    {
        if (!Enum.TryParse<ReactionTargetType>(targetTypeStr, true, out var targetType))
        {
            throw new ArgumentException("Invalid target type");
        }

        await EnsureUserHasAccess(targetId, targetType, userId);

        var existing = await _reactionRepository.Find(targetId, targetType, userId, content);

        if (existing != null)
        {
            await _reactionRepository.Delete(existing);
            await _userStatRepository.RecalculateByUserId(existing.UserId);
        }
        else
        {
            var reaction = new Reaction
            {
                TargetId = targetId,
                TargetType = targetType,
                UserId = userId,
                Content = content,
                CreatedOn = DateTime.UtcNow
            };
            await _reactionRepository.Add(reaction);

            var actor = await _applicationUserRepository.FindById(userId);
            var actorName = actor?.Name ?? actor?.UserName ?? "Someone";

            if (targetType == ReactionTargetType.Event)
            {
                var evt = await _eventRepository.GetById(targetId);
                if (evt != null && evt.CreatorId != userId)
                {
                    await _notificationService.CreateNotificationAsync(
                        evt.CreatorId,
                        "Nieuwe reactie",
                        $"{actorName} heeft gereageerd op jouw evenement '{evt.Title}'.",
                        $"/events/{evt.Id}",
                        true
                    );
                }
            }
            else if (targetType == ReactionTargetType.Reaction)
            {
                var parentReaction = await _reactionRepository.GetById(targetId);
                if (parentReaction != null && parentReaction.UserId != userId)
                {
                    var rootId = targetId;
                    var rootType = targetType;
                    while (rootType == ReactionTargetType.Reaction)
                    {
                        var parent = await _reactionRepository.GetById(rootId);
                        if (parent == null) break;
                        rootId = parent.TargetId;
                        rootType = parent.TargetType;
                    }
                    var link = rootType == ReactionTargetType.Event ? $"/events/{rootId}" : null;

                    await _notificationService.CreateNotificationAsync(
                        parentReaction.UserId,
                        "Nieuw antwoord",
                        $"{actorName} heeft geantwoord op je reactie.",
                        link,
                        true
                    );
                }
            }
            
            await _eventDispatcher.DispatchAsync(new ReactionAddedEvent(reaction));
        }
    }

    public async Task<IEnumerable<ReactionDto>> GetReactionsForTarget(Guid targetId, string targetTypeStr)
    {
        if (!Enum.TryParse<ReactionTargetType>(targetTypeStr, true, out var targetType))
        {
            throw new ArgumentException("Invalid target type");
        }

        var topLevelReactions = await _reactionRepository.GetByTargets(targetType, [targetId]);

        var topLevelDtos = topLevelReactions.Select(MapToDto).ToList();

        if (topLevelDtos.Any())
        {
            var topLevelIds = topLevelDtos.Select(r => r.Id).ToList();
            
            // Level 2: Replies targeting Level 1
            var level2Reactions = await _reactionRepository.GetByTargets(ReactionTargetType.Reaction, topLevelIds);

            var level2Dtos = level2Reactions.Select(r => {
                var dto = MapToDto(r);
                var target = topLevelDtos.FirstOrDefault(t => t.Id == r.TargetId);
                dto.TargetUserName = target?.UserName;
                return dto;
            }).ToList();

            if (level2Dtos.Any())
            {
                var level2Ids = level2Dtos.Select(r => r.Id).ToList();
                
                // Level 3: Replies targeting Level 2
                var level3Reactions = await _reactionRepository.GetByTargets(ReactionTargetType.Reaction, level2Ids);

                var level3Dtos = level3Reactions.Select(r => {
                    var dto = MapToDto(r);
                    var target = level2Dtos.FirstOrDefault(t => t.Id == r.TargetId);
                    dto.TargetUserName = target?.UserName;
                    return dto;
                }).ToList();

                // Group them by Parent (Level 1)
                // For a "Flat thread" feel, all level 2 and level 3 replies will be shown under the level 1 root.
                foreach (var parent in topLevelDtos)
                {
                    var directReplies = level2Dtos.Where(r => r.TargetId == parent.Id).ToList();
                    var indirectReplies = level3Dtos.Where(r => directReplies.Any(dr => dr.Id == r.TargetId)).ToList();
                    
                    parent.Reactions = directReplies.Concat(indirectReplies).OrderBy(r => r.CreatedOn).ToList();
                }
            }
        }

        return topLevelDtos;
    }

    public async Task<IEnumerable<ReactionDto>> GetTimeline(int skip, int take)
    {
        var reactions = await _reactionRepository.GetTimeline(skip, take);

        var dtos = reactions.Select(MapToDto).ToList();

        // Optional: Eagerly load children for timeline if needed, 
        // but for a flat timeline we usually just show the main content.
        // Let's at least load the count or immediate children if they are reactions.
        
        return dtos;
    }

    public async Task DeleteReaction(Guid id, string userId, bool isAdmin)
    {
        var reaction = await _reactionRepository.GetById(id);
        if (reaction == null) return;

        // Admins can delete anything, users only their own
        if (!isAdmin && reaction.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this reaction.");
        }

        var affectedUserIds = await _reactionRepository.DeleteWithReplies(id);
        foreach (var affectedUserId in affectedUserIds)
        {
            await _userStatRepository.RecalculateByUserId(affectedUserId);
        }
    }

    private static ReactionDto MapToDto(Reaction r) => new ReactionDto
    {
        Id = r.Id,
        UserId = r.UserId,
        UserName = r.User?.UserName ?? "Unknown",
        ProfilePictureUrl = r.User?.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{r.User.ProfileImageId}" : null,
        Content = r.Content,
        TargetId = r.TargetId,
        TargetType = r.TargetType.ToString(),
        CreatedOn = r.CreatedOn
    };

    private async Task EnsureUserHasAccess(Guid targetId, ReactionTargetType targetType, string userId)
    {
        // Trace back to root target if this is a reply to another reaction
        while (targetType == ReactionTargetType.Reaction)
        {
            var parent = await _reactionRepository.GetById(targetId);
            if (parent == null) throw new ArgumentException("Parent reaction not found");
            targetId = parent.TargetId;
            targetType = parent.TargetType;
        }

        if (targetType == ReactionTargetType.Event)
        {
            var evt = await _eventRepository.GetById(targetId);

            if (evt == null) throw new ArgumentException("Event not found");

            if (evt.RequiredRoles.Any())
            {
                var userRoles = await _userService.GetUserRoles(userId);
                if (!userRoles.Contains(Roles.Admin) && !evt.RequiredRoles.Any(r => userRoles.Contains(r.Name!)))
                {
                    throw new UnauthorizedAccessException("You don't have the required role to react to this event.");
                }
            }
        }
    }
}
