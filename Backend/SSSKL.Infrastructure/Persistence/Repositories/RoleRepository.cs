using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;

namespace SSSKL.Infrastructure.Persistence.Repositories;

// Uses the scoped context so returned roles are tracked by the same unit of work as the
// entity they get attached to (e.g. quotes), instead of being inserted again.
public class RoleRepository(ApplicationDbContext dbContext) : IRoleRepository
{
    public async Task<IList<IdentityRole>> GetAll()
    {
        return await dbContext.Roles.ToListAsync();
    }

    public async Task<IList<IdentityRole>> GetByNames(IEnumerable<string> names)
    {
        return await dbContext.Roles
            .Where(r => names.Contains(r.Name!))
            .ToListAsync();
    }

    public async Task<IList<string>> GetUserIdsInRoles(IEnumerable<string> roleNames)
    {
        return await dbContext.UserRoles
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => roleNames.Contains(x.Name!))
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync();
    }
}
