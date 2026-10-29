using MediatR;
using SSSKL.Contracts.Dto.Api.v1;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievementById;

public sealed record GetAchievementByIdQuery(Guid Id) : IRequest<AchievementResponseDto>;
