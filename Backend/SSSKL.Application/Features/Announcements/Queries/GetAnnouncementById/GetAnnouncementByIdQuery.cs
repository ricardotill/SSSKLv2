using MediatR;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Announcements.Queries.GetAnnouncementById;

public sealed record GetAnnouncementByIdQuery(Guid Id) : IRequest<Announcement>;
