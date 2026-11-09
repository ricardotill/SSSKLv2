using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Announcements.Commands.UpdateAnnouncement;

public sealed record UpdateAnnouncementCommand(Guid Id, AnnouncementUpdateDto Announcement) : IRequest;
