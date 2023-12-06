using SSSKLv2.Data;
using SSSKLv2.DTO;

namespace SSSKLv2.Services.Interfaces;

public interface IApplicationUserService
{
    public Task<ApplicationUser> GetUserById(string id);
    public Task<ApplicationUser> GetUserByUsername(string username);
    public Task<IList<ApplicationUser>> GetAllUsers();
    public Task<ApplicationUserPaged> GetPagedUsers(string searchParam, int page);
}