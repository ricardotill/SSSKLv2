using SSSKLv2.Data;
using SSSKLv2.Dto;

namespace SSSKLv2.Services.Interfaces;

public interface IApplicationUserService
{
    public Task<int> GetCount();
    public Task<int> GetCountAdmin();
    public Task<ApplicationUser> GetUserById(string id);
    public Task<ApplicationUser> GetUserByUsername(string username);
    public Task<IList<ApplicationUser>> GetAllUsers();
    // Paged overload - return only the requested users (Skip/Take)
    public Task<IList<ApplicationUser>> GetAllUsers(int skip, int take);
    public Task<IList<ApplicationUser>> GetAllUsersAdmin(int skip, int take);
    public Task<IEnumerable<LeaderboardEntryDto>> GetAllLeaderboard(Guid productId);
    public Task<IEnumerable<LeaderboardEntryDto>> GetMonthlyLeaderboard(Guid productId);
    public Task<IEnumerable<LeaderboardEntryDto>> Get12HourlyLeaderboard(Guid productId);
    public Task<IEnumerable<LeaderboardEntryDto>> Get12HourlyLiveLeaderboard(Guid productId);
    // Update an existing user with values from the DTO. Returns the updated ApplicationUser.
    public Task<ApplicationUser> UpdateUser(string id, SSSKLv2.Dto.Api.v1.ApplicationUserUpdateDto dto);
    // Delete an existing user by id.
    public Task DeleteUser(string id);
    // Returns the list of roles assigned to the user with the given id.
    public Task<IList<string>> GetUserRoles(string userId);

    public Task UpdateProfilePictureAsync(string userId, Stream imageStream, string contentType);
    public Task DeleteProfilePictureAsync(string userId);
}

