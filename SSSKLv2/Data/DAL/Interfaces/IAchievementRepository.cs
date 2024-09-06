namespace SSSKLv2.Data.DAL.Interfaces;

public interface IAchievementRepository
{
    Task<IEnumerable<Achievement>> GetAll();
    Task<IEnumerable<AchievementEntry>> GetAllEntriesOfAchievement(Guid achievementId);
    Task<IEnumerable<AchievementEntry>> GetAllEntriesOfUser(string userId);
    Task<Achievement> GetById(Guid id);
    Task Create(Achievement achievement);
    Task Update(Achievement achievement);
    Task Delete(Guid id);
}