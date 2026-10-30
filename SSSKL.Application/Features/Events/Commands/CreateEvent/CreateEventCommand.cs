using MediatR;
using SSSKL.Contracts.Dto.Api;

namespace SSSKL.Application.Features.Events.Commands.CreateEvent;

/// <summary>Creates the event and returns it as the creator sees it.</summary>
public sealed record CreateEventCommand(EventCreateDto Event, string CreatorId) : IRequest<EventDto>;
