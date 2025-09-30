namespace SSSKLv2.Data.DAL.Interfaces;

public interface IAchievementRepository
{
    Task<IEnumerable<Achievement>> GetAll();
    IQueryable<Achievement> GetAllQueryable(ApplicationDbContext context);
    Task<IEnumerable<AchievementEntry>> GetAllEntries(Guid achievementId);
    IQueryable<AchievementEntry> GetAllEntriesQueryable(ApplicationDbContext context);
    Task<IEnumerable<AchievementEntry>> GetAllEntriesOfUser(string userId);
    Task<Achievement> GetById(Guid id);
    Task Create(Achievement achievement);
    Task CreateEntryRange(IEnumerable<AchievementEntry> entries);
    Task Update(Achievement achievement);
    Task Delete(Guid id);
    Task<IEnumerable<Achievement>> GetUncompletedAchievementsForUser(string userId);
}