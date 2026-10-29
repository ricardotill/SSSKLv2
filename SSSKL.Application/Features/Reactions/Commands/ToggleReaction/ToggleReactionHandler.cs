using MediatR;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;

namespace SSSKL.Application.Features.Reactions.Commands.ToggleReaction;

internal sealed class ToggleReactionHandler(
    IReactionRepository reactionRepository,
    IEventRepository eventRepository,
    IApplicationUserRepository applicationUserRepository,
    IUserRoleProvider roleProvider,
    IUserNotifier userNotifier,
    IDomainEventDispatcher eventDispatcher,
    IUserStatRepository userStatRepository) : IRequestHandler<ToggleReactionCommand>
{
    public async Task Handle(ToggleReactionCommand request, CancellationToken cancellationToken)
    {
        var targetType = ReactionMappings.ParseTargetType(request.TargetType);
        var targetId = request.TargetId;
        var userId = request.UserId;

        await EnsureUserHasAccess(targetId, targetType, userId);

        var existing = await reactionRepository.Find(targetId, targetType, userId, request.Content);
        if (existing != null)
        {
            await reactionRepository.Delete(existing);
            await userStatRepository.RecalculateByUserId(existing.UserId);
            return;
        }

        var reaction = new Reaction
        {
            TargetId = targetId,
            TargetType = targetType,
            UserId = userId,
            Content = request.Content,
            CreatedOn = DateTime.UtcNow
        };
        await reactionRepository.Add(reaction);

        var actor = await applicationUserRepository.FindById(userId);
        var actorName = actor?.Name ?? actor?.UserName ?? "Someone";

        if (targetType == ReactionTargetType.Event)
        {
            await NotifyEventCreator(targetId, userId, actorName);
        }
        else if (targetType == ReactionTargetType.Reaction)
        {
            await NotifyParentAuthor(targetId, userId, actorName);
        }

        await eventDispatcher.DispatchAsync(new ReactionAddedEvent(reaction));
    }

    private async Task NotifyEventCreator(Guid eventId, string userId, string actorName)
    {
        var evt = await eventRepository.GetById(eventId);
        if (evt == null || evt.CreatorId == userId) return;

        await userNotifier.NotifyAsync(
            evt.CreatorId,
            "Nieuwe reactie",
            $"{actorName} heeft gereageerd op jouw evenement '{evt.Title}'.",
            $"/events/{evt.Id}",
            sendPush: true);
    }

    private async Task NotifyParentAuthor(Guid parentReactionId, string userId, string actorName)
    {
        var parentReaction = await reactionRepository.GetById(parentReactionId);
        if (parentReaction == null || parentReaction.UserId == userId) return;

        // Link to the event at the root of the thread, if there is one
        var (rootId, rootType) = await FindRoot(parentReactionId, ReactionTargetType.Reaction);
        var link = rootType == ReactionTargetType.Event ? $"/events/{rootId}" : null;

        await userNotifier.NotifyAsync(
            parentReaction.UserId,
            "Nieuw antwoord",
            $"{actorName} heeft geantwoord op je reactie.",
            link,
            sendPush: true);
    }

    private async Task<(Guid Id, ReactionTargetType Type)> FindRoot(Guid targetId, ReactionTargetType targetType)
    {
        while (targetType == ReactionTargetType.Reaction)
        {
            var parent = await reactionRepository.GetById(targetId);
            if (parent == null) break;
            targetId = parent.TargetId;
            targetType = parent.TargetType;
        }

        return (targetId, targetType);
    }

    private async Task EnsureUserHasAccess(Guid targetId, ReactionTargetType targetType, string userId)
    {
        // Trace back to root target if this is a reply to another reaction
        while (targetType == ReactionTargetType.Reaction)
        {
            var parent = await reactionRepository.GetById(targetId) ?? throw new ArgumentException("Parent reaction not found");
            targetId = parent.TargetId;
            targetType = parent.TargetType;
        }

        if (targetType != ReactionTargetType.Event) return;

        var evt = await eventRepository.GetById(targetId) ?? throw new ArgumentException("Event not found");
        if (!evt.RequiredRoles.Any()) return;

        var userRoles = await roleProvider.GetRoles(userId);
        if (!userRoles.Contains(Roles.Admin) && !evt.RequiredRoles.Any(r => userRoles.Contains(r.Name!)))
        {
            throw new UnauthorizedAccessException("You don't have the required role to react to this event.");
        }
    }
}
