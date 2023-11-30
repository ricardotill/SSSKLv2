using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface ITopUpService
{
    public Task<PaginationObject<TopUp>> GetAllPagination(int page);
    public Task<PaginationObject<TopUp>> GetPersonalPagination(int page, string id);
    public Task<TopUp> GetById(string id);
    public Task CreateTopUp(TopUp topup);
    public Task DeleteTopUp(Guid id);
}