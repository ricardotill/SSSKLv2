using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SSSKLv2.Data;

namespace SSSKLv2.Components.Account;

public enum IdentityClaim
{
    Name,
    Saldo
}

public class IdentityClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public IdentityClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(IdentityClaim.Name.ToString(), user.Name));
        identity.AddClaim(new Claim(IdentityClaim.Saldo.ToString(), user.Saldo.ToString("C")));
        return identity;
    }
}