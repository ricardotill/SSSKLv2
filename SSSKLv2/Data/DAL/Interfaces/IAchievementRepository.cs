namespace SSSKLv2.Data.DAL.Interfaces;

public interface IAchievementRepository
{
    Task<IEnumerable<Achievement>> GetAll();
    IQueryable<Achievement> GetAllQueryable(ApplicationDbContext context);
    Task<Achievement> GetById(Guid id);
    Task<IEnumerable<AchievementEntry>> GetAllEntries(Guid achievementId);
    IQueryable<AchievementEntry> GetAllEntriesQueryable(ApplicationDbContext context);
    Task<IList<AchievementEntry>> GetAllEntriesOfUser(string userId);
    Task Create(Achievement achievement);
    Task CreateEntryRange(IEnumerable<AchievementEntry> entries);
    Task Update(Achievement achievement);
    Task Delete(Guid id);
    Task DeleteAchievementEntryRange(IEnumerable<AchievementEntry> entries);
    Task<IEnumerable<Achievement>> GetUncompletedAchievementsForUser(string userId);
}