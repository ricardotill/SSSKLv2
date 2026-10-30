using MediatR;

namespace SSSKL.Application.Features.Events.Commands.DeleteEvent;

/// <summary>Only the creator or an admin may delete the event.</summary>
public sealed record DeleteEventCommand(Guid Id, string UserId, bool IsAdmin) : IRequest;
