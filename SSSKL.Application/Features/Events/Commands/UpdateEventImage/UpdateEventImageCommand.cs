using MediatR;

namespace SSSKL.Application.Features.Events.Commands.UpdateEventImage;

/// <summary>Replaces the event image. Only the creator or an admin may do this.</summary>
public sealed record UpdateEventImageCommand(Guid Id, string UserId, bool IsAdmin, Stream ImageContent, string ContentType) : IRequest;
