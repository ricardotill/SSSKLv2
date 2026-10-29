using MediatR;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Users.Queries.GetUserDetails;

internal sealed class GetUserDetailsHandler(
    IApplicationUserRepository applicationUserRepository,
    IUserRoleProvider roleProvider) : IRequestHandler<GetUserDetailsQuery, ApplicationUserDetailedDto>
{
    public async Task<ApplicationUserDetailedDto> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var user = await request.User.Resolve(applicationUserRepository);
        var roles = await roleProvider.GetRoles(user.Id);
        return user.ToDetailedDto(roles);
    }
}
