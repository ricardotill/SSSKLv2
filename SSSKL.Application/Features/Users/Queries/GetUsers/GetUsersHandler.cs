using MediatR;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;

namespace SSSKL.Application.Features.Users.Queries.GetUsers;

internal sealed class GetUsersHandler(IApplicationUserRepository applicationUserRepository)
    : IRequestHandler<GetUsersQuery, PaginationObject<ApplicationUserDto>>
{
    public async Task<PaginationObject<ApplicationUserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var users = request.ForAdmin
            ? await applicationUserRepository.GetAllForAdminPaged(request.Skip, request.Take)
            : await applicationUserRepository.GetAllPaged(request.Skip, request.Take);
        var totalCount = request.ForAdmin
            ? await applicationUserRepository.GetCountAll()
            : await applicationUserRepository.GetCount();

        return new PaginationObject<ApplicationUserDto>
        {
            Items = users.Select(u => u.ToDto()).ToList(),
            TotalCount = totalCount
        };
    }
}
