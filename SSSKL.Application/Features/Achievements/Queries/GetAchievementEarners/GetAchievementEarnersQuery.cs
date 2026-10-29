using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievementEarners;

public sealed record GetAchievementEarnersQuery(Guid AchievementId) : IRequest<IReadOnlyList<AchievementEntryDto>>;
