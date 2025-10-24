namespace SSSKLv2.Data.DAL.Interfaces;

public interface IApplicationUserRepository
{
    Task<int> GetCount();
    public Task<IList<ApplicationUser>> GetAll();
    // Paged overload - return only the requested users (Skip/Take)
    public Task<IList<ApplicationUser>> GetAllPaged(int skip, int take);
    public Task<IList<ApplicationUser>> GetAllWithOrders();
    public Task<IList<ApplicationUser>> GetFirst12WithOrders();
    public Task<ApplicationUser> GetById(string id);
    public Task<ApplicationUser> GetByUsername(string username);
    public Task<IList<ApplicationUser>> GetAllForAdmin();
}