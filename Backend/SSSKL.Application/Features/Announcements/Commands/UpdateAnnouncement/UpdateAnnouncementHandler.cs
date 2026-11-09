using MediatR;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Announcements.Commands.UpdateAnnouncement;

internal sealed class UpdateAnnouncementHandler(IAnnouncementRepository announcementRepository)
    : IRequestHandler<UpdateAnnouncementCommand>
{
    public async Task Handle(UpdateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Announcement;
        var existing = await announcementRepository.GetExisting(request.Id);

        existing.Message = dto.Message;
        existing.Description = dto.Description;
        existing.Order = dto.Order;
        existing.IsScheduled = dto.IsScheduled;
        existing.PlannedFrom = dto.PlannedFrom;
        existing.PlannedTill = dto.PlannedTill;

        await announcementRepository.Update(existing);
    }
}
