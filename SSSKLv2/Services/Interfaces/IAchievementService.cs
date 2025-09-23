using SSSKLv2.Data;
using SSSKLv2.Dto;


namespace SSSKLv2.Services.Interfaces;

public interface IAchievementService
{
    Task<List<AchievementDto>> GetPersonalAchievements(string userId);
    Task<IEnumerable<Achievement>> GetAchievements();
    Task CheckOrderForAchievements(Order order);
    Task<bool> AwardAchievementToUser(string userId, Guid achievementId);
    Task<int> AwardAchievementToAllUsers(Guid achievementId);
}