using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Announcements.Commands.DeleteAnnouncement;

internal sealed class DeleteAnnouncementHandler(IAnnouncementRepository announcementRepository)
    : IRequestHandler<DeleteAnnouncementCommand>
{
    public async Task Handle(DeleteAnnouncementCommand request, CancellationToken cancellationToken)
    {
        await announcementRepository.GetExisting(request.Id);
        await announcementRepository.Delete(request.Id);
    }
}
