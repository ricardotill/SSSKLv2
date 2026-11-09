using MediatR;

namespace SSSKL.Application.Features.Announcements.Commands.DeleteAnnouncement;

public sealed record DeleteAnnouncementCommand(Guid Id) : IRequest;
