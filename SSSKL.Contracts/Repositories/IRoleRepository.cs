using Microsoft.AspNetCore.Identity;

namespace SSSKL.Contracts.Repositories;

public interface IRoleRepository
{
    Task<IList<IdentityRole>> GetByNames(IEnumerable<string> names);
    Task<IList<string>> GetUserIdsInRoles(IEnumerable<string> roleNames);
}
