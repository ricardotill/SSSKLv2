using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class ApplicationUserService(IApplicationUserRepository _applicationUserRepository) : IApplicationUserService
{
    public async Task<ApplicationUser> GetUserById(string id)
    {
        return await _applicationUserRepository.GetById(id);
    }
    public async Task<ApplicationUser> GetUserByUsername(string username)
    {
        return await _applicationUserRepository.GetByUsername(username);
    }
    public async Task<IList<ApplicationUser>> GetAllUsers()
    {
        return await _applicationUserRepository.GetAll();
    }
    public async Task<IQueryable<ApplicationUser>> GetAllUsersObscured()
    {
        var result = new List<ApplicationUser>();

        var list = await _applicationUserRepository.GetAll();

        foreach (var item in list)
        {
            ApplicationUser objApplicationUser = new ApplicationUser();

            objApplicationUser.Id = item.Id;
            objApplicationUser.UserName = item.UserName;
            objApplicationUser.Saldo = item.Saldo;
            objApplicationUser.Email = item.Email;
            objApplicationUser.Name = item.Name;
            objApplicationUser.Surname = item.Surname;
            objApplicationUser.EmailConfirmed = item.EmailConfirmed;
            objApplicationUser.PhoneNumber = item.PhoneNumber;
            objApplicationUser.PasswordHash = "*****";

            result.Add(objApplicationUser);
        }

        return result.AsQueryable();
    }
}