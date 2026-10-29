using Microsoft.AspNetCore.Identity;

namespace SSSKLv2.Data.DAL.Interfaces;

public interface IRoleRepository
{
    Task<IList<IdentityRole>> GetByNames(IEnumerable<string> names);
    Task<IList<string>> GetUserIdsInRoles(IEnumerable<string> roleNames);
}
