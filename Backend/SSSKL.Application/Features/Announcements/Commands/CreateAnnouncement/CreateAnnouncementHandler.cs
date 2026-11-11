using MediatR;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Announcements.Commands.CreateAnnouncement;

internal sealed class CreateAnnouncementHandler(IAnnouncementRepository announcementRepository)
    : IRequestHandler<CreateAnnouncementCommand, Announcement>
{
    public async Task<Announcement> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Announcement;
        var announcement = new Announcement
        {
            Message = dto.Message,
            Description = dto.Description,
            Order = dto.Order,
            IsScheduled = dto.IsScheduled,
            PlannedFrom = dto.PlannedFrom,
            PlannedTill = dto.PlannedTill
        };

        await announcementRepository.Create(announcement);
        return announcement;
    }
}
