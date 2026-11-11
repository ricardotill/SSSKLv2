using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Announcements.Queries.GetAnnouncements;

public sealed record GetAnnouncementsQuery(int Skip, int Take) : IRequest<PaginationObject<Announcement>>;
