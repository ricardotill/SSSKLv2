using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class TopUpService(ITopUpRepository _topUpRepository) : ITopUpService
{
    public async Task<PaginationObject<TopUp>> GetAllPagination(int page)
    {
        return await _topUpRepository.GetAllPagination(page);
    }
    public async Task<PaginationObject<TopUp>> GetPersonalPagination(int page, string id)
    {
        return await _topUpRepository.GetPersonalPagination(page, id);
    }
    public async Task<TopUp> GetById(string id)
    {
        return await _topUpRepository.GetById(Guid.Parse(id));
    }
    public async Task CreateTopUp(TopUp topup)
    {
        await _topUpRepository.Update(topup);
    }
    public async Task DeleteTopUp(Guid id)
    {
        await _topUpRepository.Delete(id);
    }
}