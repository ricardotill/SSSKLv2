using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Achievements.Queries.GetAchievementById;

internal sealed class GetAchievementByIdHandler(IAchievementRepository achievementRepository)
    : IRequestHandler<GetAchievementByIdQuery, AchievementResponseDto>
{
    public async Task<AchievementResponseDto> Handle(GetAchievementByIdQuery request, CancellationToken cancellationToken)
    {
        var achievement = await achievementRepository.GetById(request.Id);
        return achievement.ToResponseDto();
    }
}
