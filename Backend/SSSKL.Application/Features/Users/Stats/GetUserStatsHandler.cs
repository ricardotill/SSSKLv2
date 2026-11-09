using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Users.Stats;

internal sealed class GetUserStatsHandler(
    IApplicationUserRepository applicationUserRepository,
    IUserStatRepository userStatRepository) : IRequestHandler<GetUserStatsQuery, UserStatDto>
{
    public async Task<UserStatDto> Handle(GetUserStatsQuery request, CancellationToken cancellationToken)
    {
        await StatsAccess.EnsureCanAccess(applicationUserRepository, request.UserId, request.RequesterId, request.IsAdmin);

        var stats = await userStatRepository.GetOrCreateByUserId(request.UserId);
        return stats.ToDto();
    }
}
