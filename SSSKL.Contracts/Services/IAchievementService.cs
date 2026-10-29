using SSSKL.Contracts.Dto;
using SSSKL.Domain.Entities;



namespace SSSKL.Contracts.Services;

public interface IAchievementService
{
    Task<int> GetCount();
    Task<IList<AchievementListingDto>> GetPersonalAchievements(string userId);
    Task<IList<AchievementListingDto>> GetPersonalAchievementsByUsername(string username);
    Task<IList<AchievementEntry>> GetPersonalAchievementEntries(string userId);
    Task<IList<AchievementEntry>> GetPersonalAchievementEntriesByUsername(string username);
    Task<IList<AchievementEntry>> GetPersonalUnseenAchievementEntries(string username);
    Task<IList<AchievementEntry>> GetEntriesForAchievement(Guid achievementId);
    Task<IEnumerable<Achievement>> GetAchievements();
    Task<IList<Achievement>> GetAchievements(int skip, int take);
    Task<Achievement> GetAchievementById(Guid id);
    Task UpdateAchievement(Achievement achievement);
    Task UpdateAchievement(Achievement achievement, Stream newImageContent, string newImageContentType);
    Task DeleteAchievement(Guid id);
    Task DeleteAchievementEntryRange(IEnumerable<AchievementEntry> entries);
    Task CheckOrdersForAchievements(IEnumerable<Order> orders);
    Task CheckTopUpForAchievements(TopUp topUp);
    Task CheckUserForAchievements(string username);
    Task<bool> AwardAchievementToUser(string userId, Guid achievementId);
    Task<int> AwardAchievementToAllUsers(Guid achievementId);
    Task AddAchievement(AchievementDto dto);
}