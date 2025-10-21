using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class TopUpService(
    ITopUpRepository _topUpRepository,
    IAchievementService _achievementService,
    ILogger<TopUpService> _logger) : ITopUpService
{
    public IQueryable<TopUp> GetAllQueryable(ApplicationDbContext dbContext)
    {
        _logger.LogInformation($"{GetType()}: Get All TopUps as Queryable");
        return _topUpRepository.GetAllQueryable(dbContext);
    }
    public IQueryable<TopUp> GetPersonalQueryable(string username, ApplicationDbContext dbContext)
    {
        _logger.LogInformation($"{GetType()}: Get Personal TopUps as Queryable for user with username {username}");
        return _topUpRepository.GetPersonalQueryable(username, dbContext);
    }
    public async Task<TopUp> GetById(string id)
    {
        _logger.LogInformation($"{GetType()}: Get TopUp with ID {id}");
        return await _topUpRepository.GetById(Guid.Parse(id));
    }
    public async Task CreateTopUp(TopUp topup)
    {
        _logger.LogInformation($"{GetType()}: Create TopUp for user {topup.User.UserName} with amount {topup.Saldo}");
        await _topUpRepository.Create(topup);
        await _achievementService.CheckTopUpForAchievements(topup);
        await _achievementService.CheckUserForAchievements(topup.User.UserName!);
    }
    public async Task DeleteTopUp(Guid id)
    {
        _logger.LogInformation($"{GetType()}: Delete TopUp with ID {id}");
        await _topUpRepository.Delete(id);
    }
}