namespace SSSKLv2.Data.DAL.Interfaces;

public interface ITopUpRepository : IRepository<TopUp>
{
    public Task<PaginationObject<TopUp>> GetAllPagination(int page);
    public Task<PaginationObject<TopUp>> GetPersonalPagination(int page, string id);
}