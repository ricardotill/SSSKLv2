using MediatR;
using Microsoft.AspNetCore.Identity;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.RoleManagement.Commands.CreateRole;

internal sealed class CreateRoleHandler(RoleManager<IdentityRole> roleManager)
    : IRequestHandler<CreateRoleCommand, RoleResult>
{
    public async Task<RoleResult> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await roleManager.RoleExistsAsync(request.Name))
            throw new ConflictException($"Role '{request.Name}' already exists.");

        var newRole = new IdentityRole(request.Name);
        var result = await roleManager.CreateAsync(newRole);

        return result.Succeeded
            ? RoleResult.Success(new RoleDto { Id = newRole.Id, Name = request.Name })
            : RoleResult.Failed(result);
    }
}
