using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using SSSKLv2.Data;

namespace SSSKLv2.Components.Account
{
    internal sealed class IdentityUserAccessor(UserManager<ApplicationUser> userManager, IdentityRedirectManager redirectManager, AuthenticationStateProvider authenticationStateProvider)
    {
        public async Task<ApplicationUser> GetRequiredUserAsync(HttpContext context)
        {
            var state = await authenticationStateProvider.GetAuthenticationStateAsync();
            var user = await userManager.GetUserAsync(state.User);

            if (user is null)
            {
                redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(state.User)}'.", context);
            }

            return user;
        }
    }
}
