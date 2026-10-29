using MediatR;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;

namespace SSSKL.Application.Features.RoleManagement.Queries.GetRoles;

internal sealed class GetRolesHandler(IRoleRepository roleRepository)
    : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task<IReadOnlyList<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.GetAll();

        return roles
            .Where(r => request.IncludeProtected || !Roles.AllProtected.Contains(r.Name))
            .Select(r => new RoleDto { Id = r.Id, Name = r.Name ?? string.Empty })
            .ToList();
    }
}
