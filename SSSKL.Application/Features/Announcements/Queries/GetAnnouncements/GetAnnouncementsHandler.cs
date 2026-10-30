using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Announcements.Queries.GetAnnouncements;

internal sealed class GetAnnouncementsHandler(IAnnouncementRepository announcementRepository)
    : IRequestHandler<GetAnnouncementsQuery, PaginationObject<Announcement>>
{
    public async Task<PaginationObject<Announcement>> Handle(GetAnnouncementsQuery request, CancellationToken cancellationToken)
    {
        var items = await announcementRepository.GetAllPaged(request.Skip, request.Take);
        var totalCount = await announcementRepository.GetCount();

        return new PaginationObject<Announcement>
        {
            Items = items,
            TotalCount = totalCount
        };
    }
}
