using MediatR;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Announcements.Queries.GetAnnouncementById;

internal sealed class GetAnnouncementByIdHandler(IAnnouncementRepository announcementRepository)
    : IRequestHandler<GetAnnouncementByIdQuery, Announcement>
{
    public async Task<Announcement> Handle(GetAnnouncementByIdQuery request, CancellationToken cancellationToken) =>
        await announcementRepository.GetExisting(request.Id);
}
