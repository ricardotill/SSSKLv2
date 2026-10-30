using MediatR;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Events.Commands.RespondToEvent;

public sealed record RespondToEventCommand(Guid Id, string UserId, EventResponseStatus Status) : IRequest;
