using SSSKLv2.Data;
using SSSKLv2.Dto;


namespace SSSKLv2.Services.Interfaces;

public interface IAchievementService
{
    Task<List<AchievementListingDto>> GetPersonalAchievements(string userId);
    Task<IEnumerable<Achievement>> GetAchievements();
    IQueryable<Achievement> GetAchievementsQueryable(ApplicationDbContext context);
    IQueryable<AchievementEntry> GetAchievementEntriesQueryable(ApplicationDbContext context);
    Task CheckOrderForAchievements(Order order);
    Task<bool> AwardAchievementToUser(string userId, Guid achievementId);
    Task<int> AwardAchievementToAllUsers(Guid achievementId);
    Task AddAchievement(AchievementDto dto);
}