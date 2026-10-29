using MediatR;
using Microsoft.AspNetCore.Identity;

namespace SSSKL.Application.Features.RoleManagement.Commands.DeleteRole;

internal sealed class DeleteRoleHandler(RoleManager<IdentityRole> roleManager)
    : IRequestHandler<DeleteRoleCommand, RoleResult>
{
    public async Task<RoleResult> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await RoleRules.GetEditable(roleManager, request.Id, "delete");
        var result = await roleManager.DeleteAsync(role);

        return result.Succeeded ? RoleResult.Success(null) : RoleResult.Failed(result);
    }
}
