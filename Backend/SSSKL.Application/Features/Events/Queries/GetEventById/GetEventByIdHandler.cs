using MediatR;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Events.Queries.GetEventById;

internal sealed class GetEventByIdHandler(IEventRepository eventRepository)
    : IRequestHandler<GetEventByIdQuery, EventDto>
{
    public async Task<EventDto> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var e = await eventRepository.GetExisting(request.Id);
        return e.ToDto(request.UserId);
    }
}
