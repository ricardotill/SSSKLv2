using Microsoft.AspNetCore.Identity;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Features.RoleManagement;

internal static class RoleRules
{
    /// <summary>Loads a role that may be changed; system roles are protected.</summary>
    public static async Task<IdentityRole> GetEditable(RoleManager<IdentityRole> roleManager, string id, string action)
    {
        var role = await roleManager.FindByIdAsync(id) ?? throw new NotFoundException("Role not found.");

        if (Roles.AllProtected.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Cannot {action} protected system role '{role.Name}'.");

        return role;
    }
}
