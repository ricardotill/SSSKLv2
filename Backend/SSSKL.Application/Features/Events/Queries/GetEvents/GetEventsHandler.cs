using MediatR;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Events.Queries.GetEvents;

internal sealed class GetEventsHandler(IEventRepository eventRepository, IUserRoleProvider roleProvider)
    : IRequestHandler<GetEventsQuery, PaginationObject<EventDto>>
{
    public async Task<PaginationObject<EventDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var (userRoles, isAdmin) = await EventAccess.GetUserAccess(roleProvider, request.UserId);
        var events = await eventRepository.GetAll(request.Skip, request.Take, request.FutureOnly, userRoles, isAdmin, request.RequiredRole);
        var totalCount = await eventRepository.GetCount(request.FutureOnly, userRoles, isAdmin, request.RequiredRole);

        return new PaginationObject<EventDto>
        {
            Items = events.Select(e => e.ToDto(request.UserId)).ToList(),
            TotalCount = totalCount
        };
    }
}
