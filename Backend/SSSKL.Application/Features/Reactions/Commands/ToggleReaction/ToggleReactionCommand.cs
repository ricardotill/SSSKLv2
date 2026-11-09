using MediatR;

namespace SSSKL.Application.Features.Reactions.Commands.ToggleReaction;

/// <summary>Adds the reaction, or removes it when the user already reacted with the same content.</summary>
public sealed record ToggleReactionCommand(Guid TargetId, string TargetType, string Content, string UserId) : IRequest;
