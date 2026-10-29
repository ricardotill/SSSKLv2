using MediatR;
using SSSKL.Contracts.Dto.Api;

namespace SSSKL.Application.Features.Events.Commands.UpdateEvent;

/// <summary>Only the creator or an admin may update the event.</summary>
public sealed record UpdateEventCommand(Guid Id, EventCreateDto Event, string UserId, bool IsAdmin) : IRequest;
