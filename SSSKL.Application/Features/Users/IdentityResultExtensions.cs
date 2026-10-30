using Microsoft.AspNetCore.Identity;

namespace SSSKL.Application.Features.Users;

internal static class IdentityResultExtensions
{
    /// <summary>Turns a failed Identity result into the InvalidOperationException the API maps to 400.</summary>
    public static void ThrowIfFailed(this IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(";", result.Errors.Select(e => e.Description)));
    }
}
