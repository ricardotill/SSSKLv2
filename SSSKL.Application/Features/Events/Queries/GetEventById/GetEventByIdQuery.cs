using MediatR;
using SSSKL.Contracts.Dto.Api;

namespace SSSKL.Application.Features.Events.Queries.GetEventById;

public sealed record GetEventByIdQuery(Guid Id, string? UserId) : IRequest<EventDto>;
