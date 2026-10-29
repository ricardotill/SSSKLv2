using Microsoft.AspNetCore.Identity;
using SSSKL.Contracts.Dto.Api;

namespace SSSKL.Application.Features.RoleManagement;

/// <summary>The outcome of a role change: the role, or the Identity validation errors that stopped it.</summary>
public sealed record RoleResult(RoleDto? Role, IReadOnlyList<IdentityError> Errors)
{
    public bool Succeeded => Errors.Count == 0;

    public static RoleResult Success(RoleDto? role) => new(role, []);
    public static RoleResult Failed(IdentityResult result) => new(null, result.Errors.ToList());
}
