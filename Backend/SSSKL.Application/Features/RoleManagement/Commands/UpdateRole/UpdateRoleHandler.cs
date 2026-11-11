using MediatR;
using Microsoft.AspNetCore.Identity;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.RoleManagement.Commands.UpdateRole;

internal sealed class UpdateRoleHandler(RoleManager<IdentityRole> roleManager)
    : IRequestHandler<UpdateRoleCommand, RoleResult>
{
    public async Task<RoleResult> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await RoleRules.GetEditable(roleManager, request.Id, "update");

        if (await roleManager.RoleExistsAsync(request.Name) &&
            !string.Equals(role.Name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException($"Role '{request.Name}' already exists.");
        }

        role.Name = request.Name;
        var result = await roleManager.UpdateAsync(role);

        return result.Succeeded
            ? RoleResult.Success(new RoleDto { Id = role.Id, Name = role.Name })
            : RoleResult.Failed(result);
    }
}
