using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.Constants;
using SSSKLv2.Dto;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class ReactionService : IReactionService
{
    private readonly ApplicationDbContext _context;
    private readonly IApplicationUserService _userService;
    private readonly INotificationService _notificationService;

    public ReactionService(ApplicationDbContext context, IApplicationUserService userService, INotificationService notificationService)
    {
        _context = context;
        _userService = userService;
        _notificationService = notificationService;
    }

    public async Task ToggleReaction(Guid targetId, string targetTypeStr, string content, string userId)
    {
        if (!Enum.TryParse<ReactionTargetType>(targetTypeStr, true, out var targetType))
        {
            throw new ArgumentException("Invalid target type");
        }

        await EnsureUserHasAccess(targetId, targetType, userId);

        var existing = await _context.Reaction
            .FirstOrDefaultAsync(r => r.TargetId == targetId && 
                                     r.TargetType == targetType && 
                                     r.UserId == userId && 
                                     r.Content == content);

        if (existing != null)
        {
            _context.Reaction.Remove(existing);
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
            _context.Reaction.Add(reaction);
            
            var actor = await _context.Users.FindAsync(userId);
            var actorName = actor?.Name ?? actor?.UserName ?? "Someone";

            if (targetType == ReactionTargetType.Event)
            {
                var evt = await _context.Event.FirstOrDefaultAsync(e => e.Id == targetId);
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
                var parentReaction = await _context.Reaction.FirstOrDefaultAsync(r => r.Id == targetId);
                if (parentReaction != null && parentReaction.UserId != userId)
                {
                    var rootId = targetId;
                    var rootType = targetType;
                    while (rootType == ReactionTargetType.Reaction)
                    {
                        var parent = await _context.Reaction.FirstOrDefaultAsync(r => r.Id == rootId);
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
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ReactionDto>> GetReactionsForTarget(Guid targetId, string targetTypeStr)
    {
        if (!Enum.TryParse<ReactionTargetType>(targetTypeStr, true, out var targetType))
        {
            throw new ArgumentException("Invalid target type");
        }

        var topLevelReactions = await _context.Reaction
            .Include(r => r.User)
            .ThenInclude(u => u.ProfileImage)
            .Where(r => r.TargetId == targetId && r.TargetType == targetType)
            .OrderBy(r => r.CreatedOn)
            .ToListAsync();

        var topLevelDtos = topLevelReactions.Select(MapToDto).ToList();

        if (topLevelDtos.Any())
        {
            var topLevelIds = topLevelDtos.Select(r => r.Id).ToList();
            
            // Level 2: Replies targeting Level 1
            var level2Reactions = await _context.Reaction
                .Include(r => r.User)
                .ThenInclude(u => u.ProfileImage)
                .Where(r => r.TargetType == ReactionTargetType.Reaction && topLevelIds.Contains(r.TargetId))
                .OrderBy(r => r.CreatedOn)
                .ToListAsync();

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
                var level3Reactions = await _context.Reaction
                    .Include(r => r.User)
                    .ThenInclude(u => u.ProfileImage)
                    .Where(r => r.TargetType == ReactionTargetType.Reaction && level2Ids.Contains(r.TargetId))
                    .OrderBy(r => r.CreatedOn)
                    .ToListAsync();

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
        var reactions = await _context.Reaction
            .Include(r => r.User)
            .ThenInclude(u => u.ProfileImage)
            .OrderByDescending(r => r.CreatedOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var dtos = reactions.Select(MapToDto).ToList();

        // Optional: Eagerly load children for timeline if needed, 
        // but for a flat timeline we usually just show the main content.
        // Let's at least load the count or immediate children if they are reactions.
        
        return dtos;
    }

    public async Task DeleteReaction(Guid id, string userId, bool isAdmin)
    {
        var reaction = await _context.Reaction.FirstOrDefaultAsync(r => r.Id == id);
        if (reaction == null) return;

        // Admins can delete anything, users only their own
        if (!isAdmin && reaction.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this reaction.");
        }

        await DeleteReactionAndChildrenInternal(id);
        await _context.SaveChangesAsync();
    }

    private async Task DeleteReactionAndChildrenInternal(Guid id)
    {
        // Recursive deletion of all nested reactions
        var children = await _context.Reaction
            .Where(r => r.TargetId == id && r.TargetType == ReactionTargetType.Reaction)
            .Select(r => r.Id)
            .ToListAsync();

        foreach (var childId in children)
        {
            await DeleteReactionAndChildrenInternal(childId);
        }

        var reaction = await _context.Reaction.FirstOrDefaultAsync(r => r.Id == id);
        if (reaction != null)
        {
            _context.Reaction.Remove(reaction);
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
            var parent = await _context.Reaction.FirstOrDefaultAsync(r => r.Id == targetId);
            if (parent == null) throw new ArgumentException("Parent reaction not found");
            targetId = parent.TargetId;
            targetType = parent.TargetType;
        }

        if (targetType == ReactionTargetType.Event)
        {
            var evt = await _context.Event
                .Include(e => e.RequiredRoles)
                .FirstOrDefaultAsync(e => e.Id == targetId);

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
