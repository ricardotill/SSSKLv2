using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class TopUpService(
    ITopUpRepository topUpRepository,
    IAchievementService achievementService,
    ILogger<TopUpService> logger) : ITopUpService
{
    public Task<int> GetCount() => topUpRepository.GetCount();
    public Task<int> GetPersonalCount(string username) => topUpRepository.GetPersonalCount(username);
    
    public async Task<IEnumerable<TopUp>> GetAll()
    {
        logger.LogInformation($"{GetType()}: Get All TopUps");
        return await topUpRepository.GetAll();
    }
    
    public async Task<IEnumerable<TopUp>> GetAll(int skip, int take)
    {
        logger.LogInformation($"{GetType()}: Get All TopUps with skip {skip} and take {take}");
        return await topUpRepository.GetAll(skip, take);
    }
    
    public IQueryable<TopUp> GetAllQueryable(ApplicationDbContext dbContext)
    {
        logger.LogInformation($"{GetType()}: Get All TopUps as Queryable");
        return topUpRepository.GetAllQueryable(dbContext);
    }
    
    public async Task<IEnumerable<TopUp>> GetAllPersonal(string username)
    {
        logger.LogInformation($"{GetType()}: Get All Personal TopUps");
        return await topUpRepository.GetPersonal(username);
    }
    
    public async Task<IEnumerable<TopUp>> GetAllPersonal(string username, int skip, int take)
    {
        logger.LogInformation($"{GetType()}: Get All Personal TopUps with skip {skip} and take {take}");
        return await topUpRepository.GetPersonal(username, skip, take);
    }
    
    public IQueryable<TopUp> GetPersonalQueryable(string username, ApplicationDbContext dbContext)
    {
        logger.LogInformation($"{GetType()}: Get Personal TopUps as Queryable for user with username {username}");
        return topUpRepository.GetPersonalQueryable(username, dbContext);
    }
    
    public async Task<TopUp> GetById(string id)
    {
        logger.LogInformation($"{GetType()}: Get TopUp with ID {id}");
        return await topUpRepository.GetById(Guid.Parse(id));
    }
    
    public async Task CreateTopUp(TopUp topup)
    {
        logger.LogInformation($"{GetType()}: Create TopUp for user {topup.User.UserName} with amount {topup.Saldo}");
        await topUpRepository.Create(topup);
        await achievementService.CheckTopUpForAchievements(topup);
        await achievementService.CheckUserForAchievements(topup.User.UserName!);
    }
    
    public async Task DeleteTopUp(Guid id)
    {
        logger.LogInformation($"{GetType()}: Delete TopUp with ID {id}");
        await topUpRepository.Delete(id);
    }
}