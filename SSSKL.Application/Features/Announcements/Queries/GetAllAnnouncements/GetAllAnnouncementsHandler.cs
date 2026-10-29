using MediatR;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Announcements.Queries.GetAllAnnouncements;

internal sealed class GetAllAnnouncementsHandler(IAnnouncementRepository announcementRepository)
    : IRequestHandler<GetAllAnnouncementsQuery, IList<Announcement>>
{
    public Task<IList<Announcement>> Handle(GetAllAnnouncementsQuery request, CancellationToken cancellationToken) =>
        announcementRepository.GetAll();
}
