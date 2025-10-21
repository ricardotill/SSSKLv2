using SSSKLv2.Components;
using SSSKLv2.Data;
using SSSKLv2.Dto;

namespace SSSKLv2.Services.Interfaces;

public interface IApplicationUserService
{
    public Task<ApplicationUser> GetUserById(string id);
    public Task<ApplicationUser> GetUserByUsername(string username);
    public Task<IList<ApplicationUser>> GetAllUsers();
    public Task<IQueryable<ApplicationUser>> GetAllUsersObscured();
    public Task<IEnumerable<LeaderboardEntryDto>> GetAllLeaderboard(Guid productId);
    public Task<IEnumerable<LeaderboardEntryDto>> GetMonthlyLeaderboard(Guid productId);
    public Task<IEnumerable<LeaderboardEntryDto>> Get12HourlyLeaderboard(Guid productId);
    public Task<IEnumerable<LeaderboardEntryDto>> Get12HourlyLiveLeaderboard(Guid productId);

}