namespace SSSKL.Application.Shared;

/// <summary>Awards achievements and announces them (live feed + notification).</summary>
public interface IAchievementAwarder
{
    /// <summary>Awards the achievement (and any missing parent tiers) to one user.</summary>
    /// <returns>False when the user already has it or the achievement doesn't exist.</returns>
    Task<bool> AwardToUser(string userId, Guid achievementId);

    /// <summary>Awards the achievement to every user that doesn't have it yet.</summary>
    /// <returns>The number of users that received it.</returns>
    Task<int> AwardToAllUsers(Guid achievementId);
}
