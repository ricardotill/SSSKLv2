namespace SSSKLv2.Data.DAL.Interfaces;

public interface IApplicationUserRepository
{
    public Task<IList<ApplicationUser>> GetAll();
    public Task<IList<ApplicationUser>> GetAllWithOrders();
    public Task<IList<ApplicationUser>> GetFirst12WithOrders();
    public Task<ApplicationUser> GetById(string id);
    public Task<ApplicationUser> GetByUsername(string username);
    public Task<IList<ApplicationUser>> GetAllForAdmin();
}