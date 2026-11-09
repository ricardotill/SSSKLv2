using MediatR;

namespace SSSKL.Application.Features.Reactions.Commands.DeleteReaction;

/// <summary>Deletes the reaction and its replies. Admins can delete any reaction, users only their own.</summary>
public sealed record DeleteReactionCommand(Guid Id, string UserId, bool IsAdmin) : IRequest;
