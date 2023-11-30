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
        var claims = new List<Claim>()
        {
            new Claim(IdentityClaim.Name.ToString(), user.Name),
            new Claim(IdentityClaim.Saldo.ToString(), user.Saldo.ToString("C"))
        };

        var roles = await UserManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        
        identity.AddClaims(claims);
        return identity;
    }
}