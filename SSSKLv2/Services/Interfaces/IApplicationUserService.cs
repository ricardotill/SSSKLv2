using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IApplicationUserService
{
    public Task<ApplicationUser> GetUserById(string id);
    public Task<ApplicationUser> GetUserByUsername(string username);
    public Task<IList<ApplicationUser>> GetAllUsers();
    public Task<IQueryable<ApplicationUser>> GetAllUsersObscured();
}