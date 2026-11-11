using MediatR;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Announcements.Queries.GetAllAnnouncements;

public sealed record GetAllAnnouncementsQuery : IRequest<IList<Announcement>>;
