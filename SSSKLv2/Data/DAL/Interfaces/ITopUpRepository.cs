namespace SSSKLv2.Data.DAL.Interfaces;

public interface ITopUpRepository
{
    public Task<PaginationObject<TopUp>> GetAllPagination(int page);
    public Task<PaginationObject<TopUp>> GetPersonalPagination(int page, string id);
    public Task<TopUp> GetById(Guid id);
    public Task Create(TopUp topup);
    public Task Delete(Guid id);
}