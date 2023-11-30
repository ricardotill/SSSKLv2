using SSSKLv2.DTO;

namespace SSSKLv2.Services.Interfaces;

public interface IApplicationUserService
{
    public Task<ApplicationUserPaged> GetPagedUsers(string searchParam, int page);
}