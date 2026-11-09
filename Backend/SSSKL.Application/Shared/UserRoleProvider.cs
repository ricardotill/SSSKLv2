using Microsoft.AspNetCore.Identity;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Shared;

internal sealed class UserRoleProvider(UserManager<ApplicationUser> userManager) : IUserRoleProvider
{
    public async Task<IList<string>> GetRoles(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("ApplicationUser not found");
        return await userManager.GetRolesAsync(user);
    }
}
