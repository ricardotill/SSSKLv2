using SSSKLv2.Data;
using SSSKLv2.Dto;

namespace SSSKLv2.Services.Interfaces;

public interface IApplicationUserService
{
    public Task<int> GetCount();
    public Task<ApplicationUser> GetUserById(string id);
    public Task<ApplicationUser> GetUserByUsername(string username);
    public Task<IList<ApplicationUser>> GetAllUsers();
    // Paged overload - return only the requested users (Skip/Take)
    public Task<IList<ApplicationUser>> GetAllUsers(int skip, int take);
    public Task<IQueryable<ApplicationUser>> GetAllUsersObscured();
    public Task<IEnumerable<LeaderboardEntryDto>> GetAllLeaderboard(Guid productId);
    public Task<IEnumerable<LeaderboardEntryDto>> GetMonthlyLeaderboard(Guid productId);
    public Task<IEnumerable<LeaderboardEntryDto>> Get12HourlyLeaderboard(Guid productId);
    public Task<IEnumerable<LeaderboardEntryDto>> Get12HourlyLiveLeaderboard(Guid productId);
    // Update an existing user with values from the DTO. Returns the updated ApplicationUser.
    public Task<ApplicationUser> UpdateUser(string id, SSSKLv2.Dto.Api.v1.ApplicationUserUpdateDto dto);

}