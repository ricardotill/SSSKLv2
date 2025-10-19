using SSSKLv2.Data;
using SSSKLv2.Dto;


namespace SSSKLv2.Services.Interfaces;

public interface IAchievementService
{
    Task<IList<AchievementListingDto>> GetPersonalAchievements(string userId);
    Task<IList<AchievementEntry>> GetPersonalAchievementEntries(string userId);
    Task<IEnumerable<Achievement>> GetAchievements();
    Task<Achievement> GetAchievementById(Guid id);
    Task UpdateAchievement(Achievement achievement);
    Task DeleteAchievement(Guid id);
    Task DeleteAchievementEntryRange(IEnumerable<AchievementEntry> entries);
    IQueryable<Achievement> GetAchievementsQueryable(ApplicationDbContext context);
    IQueryable<AchievementEntry> GetAchievementEntriesQueryable(ApplicationDbContext context);
    Task CheckOrderForAchievements(Order order);
    Task<bool> AwardAchievementToUser(string userId, Guid achievementId);
    Task<int> AwardAchievementToAllUsers(Guid achievementId);
    Task AddAchievement(AchievementDto dto);
}