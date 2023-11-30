namespace SSSKLv2.Data.DAL.Interfaces;

public interface IApplicationUserRepository : IRepository<ApplicationUser>
{
    public Task<IList<ApplicationUser>> GetAllBySearchparam(string searchparam, int page);
}