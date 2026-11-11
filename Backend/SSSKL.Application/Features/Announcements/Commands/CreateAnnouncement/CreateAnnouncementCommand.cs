using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Announcements.Commands.CreateAnnouncement;

public sealed record CreateAnnouncementCommand(AnnouncementCreateDto Announcement) : IRequest<Announcement>;
