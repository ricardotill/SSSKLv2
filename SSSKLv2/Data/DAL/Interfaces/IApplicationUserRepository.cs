namespace SSSKLv2.Data.DAL.Interfaces;

public interface IApplicationUserRepository
{
    Task<int> GetCount();
    Task<int> GetCountAll();
    public Task<IList<ApplicationUser>> GetAll();
    // Paged overload - return only the requested users (Skip/Take)
    public Task<IList<ApplicationUser>> GetAllPaged(int skip, int take);
    public Task<IList<ApplicationUser>> GetAllForAdminPaged(int skip, int take);
    // Batch lookup of display fields for a known set of user ids (leaderboard rendering)
    public Task<IList<ApplicationUser>> GetByIds(IEnumerable<string> ids);
    // Ids of the most recently active consumer users, ordered by LastOrdered desc
    public Task<IList<string>> GetTopActiveUserIds(int take);
    public Task<ApplicationUser> GetById(string id);
    public Task<ApplicationUser> GetByUsername(string username);
    public Task<IList<ApplicationUser>> GetAllForAdmin();
}