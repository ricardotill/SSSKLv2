using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Users.Queries.GetUserProfile;

internal sealed class GetUserProfileHandler(IApplicationUserRepository applicationUserRepository)
    : IRequestHandler<GetUserProfileQuery, ApplicationUserDto>
{
    public async Task<ApplicationUserDto> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await applicationUserRepository.GetById(request.Id);
        return user.ToDto();
    }
}
