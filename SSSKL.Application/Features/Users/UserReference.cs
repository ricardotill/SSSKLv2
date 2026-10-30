using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Users;

/// <summary>Identifies a user by id (admin routes) or by username (the signed-in user's "me" routes).</summary>
public sealed record UserReference(string? Id = null, string? Username = null)
{
    public static UserReference ById(string id) => new(Id: id);
    public static UserReference ByUsername(string username) => new(Username: username);

    /// <summary>Loads the user; throws NotFoundException when it doesn't exist.</summary>
    internal Task<ApplicationUser> Resolve(IApplicationUserRepository repository) =>
        Id != null ? repository.GetById(Id) : repository.GetByUsername(Username!);
}
