using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievements;

public sealed record GetAchievementsQuery(int Skip, int Take) : IRequest<PaginationObject<AchievementResponseDto>>;
