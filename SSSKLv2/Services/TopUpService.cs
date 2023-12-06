using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class TopUpService(
    ITopUpRepository _topUpRepository) : ITopUpService
{
    public async Task<IQueryable<TopUp>> GetAllQueryable()
    {
        return await _topUpRepository.GetAllQueryable();
    }
    public async Task<IQueryable<TopUp>> GetPersonalQueryable(string username)
    {
        return await _topUpRepository.GetPersonalQueryable(username);
    }
    public async Task<TopUp> GetById(string id)
    {
        return await _topUpRepository.GetById(Guid.Parse(id));
    }
    public async Task CreateTopUp(TopUp topup)
    {
        await _topUpRepository.Create(topup);
    }
    public async Task DeleteTopUp(Guid id)
    {
        var topup = await _topUpRepository.GetById(id);
        await _topUpRepository.Delete(id);
    }
}