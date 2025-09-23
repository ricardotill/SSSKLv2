using SSSKLv2.Dto;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class AchievementService(
    IAchievementRepository achievementRepository,
    IOrderRepository orderRepository,
    ITopUpRepository topUpRepository,
    IApplicationUserRepository applicationUserRepository) : IAchievementService
{
    public async Task<List<AchievementDto>> GetPersonalAchievements(string userId)
    {
        var allAchievements = await achievementRepository.GetAll();
        var achievementEntries = await achievementRepository.GetAllEntriesOfUser(userId);
        
        return allAchievements.Select(a =>
            new AchievementDto(
                a.Name,
                a.Description,
                a.Image?.Uri,
                achievementEntries.Any(e => e.Achievement.Id == a.Id)
            )
        ).ToList();
    }

    public async Task<IEnumerable<Achievement>> GetAchievements()
    {
        return await achievementRepository.GetAll();
    }
    
    public async Task CheckOrderForAchievements(Order order)
    {
        // Get all achievements the user hasn't completed yet
        var userId = order.User.Id;
        var uncompletedAchievements = await achievementRepository.GetUncompletedAchievementsForUser(userId);
        
        // Get user's order history for calculations
        var userOrders = await orderRepository.GetPersonal(order.User.Id);
        
        var newAchievementEntries = new List<AchievementEntry>();
        
        foreach (var achievement in uncompletedAchievements)
        {
            bool shouldAward = false;
            
            switch (achievement.Action)
            {
                case Achievement.ActionOption.UserBuy:
                    // Check if user has bought enough items
                    var userTotalBought = userOrders.Sum(o => o.Amount);
                    shouldAward = CheckComparison(userTotalBought, achievement.ComparisonOperator, achievement.ComparisonValue);
                    break;
                    
                case Achievement.ActionOption.TotalBuy:
                    // Check if user's total purchase amount meets criteria
                    var userTotalSpent = userOrders.Sum(o => o.Paid);
                    shouldAward = CheckComparison((int)userTotalSpent, achievement.ComparisonOperator, achievement.ComparisonValue);
                    break;
                    
                case Achievement.ActionOption.UserTopUp:
                    // Get user's top-up history
                    var userTopUps = await topUpRepository.GetPersonal(userId);
                    var userTopUpCount = userTopUps.Count;
                    shouldAward = CheckComparison(userTopUpCount, achievement.ComparisonOperator, achievement.ComparisonValue);
                    break;
                    
                case Achievement.ActionOption.TotalTopUp:
                    // Get user's total top-up amount (using Saldo property)
                    var SumTopUps = (await topUpRepository.GetPersonal(userId)).Sum(t => t.Saldo);
                    var roundedSumTopUps = (int)Math.Round(SumTopUps);
                    shouldAward = CheckComparison(roundedSumTopUps, achievement.ComparisonOperator, achievement.ComparisonValue);
                    break;
                    
                case Achievement.ActionOption.YearsOfMembership:
                    // Calculate years of membership based on the oldest order date as a proxy for membership start
                    var oldestOrder = userOrders.OrderBy(o => o.CreatedOn).FirstOrDefault();
                    if (oldestOrder != null)
                    {
                        var membershipYears = (DateTime.Now - oldestOrder.CreatedOn).Days / 365;
                        shouldAward = CheckComparison(membershipYears, achievement.ComparisonOperator, achievement.ComparisonValue);
                    }
                    break;
            }
            
            if (shouldAward)
            {
                var achievementEntry = new AchievementEntry
                {
                    Id = Guid.NewGuid(),
                    Achievement = achievement,
                    User = order.User,
                    HasSeen = false,
                    CreatedOn = DateTime.Now
                };
                
                newAchievementEntries.Add(achievementEntry);
            }
        }
        
        // Save new achievement entries to database
        if (newAchievementEntries.Any())
        {
            await achievementRepository.CreateEntryRange(newAchievementEntries);
        }
    }
    
    public async Task<bool> AwardAchievementToUser(string userId, Guid achievementId)
    {
        // Check if user already has the achievement
        var entries = await achievementRepository.GetAllEntriesOfUser(userId);
        if (entries.Any(e => e.Achievement.Id == achievementId))
            return false; // Already awarded

        // Get achievement
        var achievement = (await achievementRepository.GetAll()).FirstOrDefault(a => a.Id == achievementId);
        if (achievement == null)
            return false; // Achievement not found

        // Create new entry
        var achievementEntry = new AchievementEntry
        {
            Id = Guid.NewGuid(),
            Achievement = achievement,
            User = new ApplicationUser { Id = userId }, // Only Id is set, assuming repo will attach
            HasSeen = false,
            CreatedOn = DateTime.Now
        };
        await achievementRepository.CreateEntryRange(new List<AchievementEntry> { achievementEntry });
        return true;
    }
    
    public async Task<int> AwardAchievementToAllUsers(Guid achievementId)
    {
        var users = await applicationUserRepository.GetAll();
        var achievement = (await achievementRepository.GetAll()).FirstOrDefault(a => a.Id == achievementId);
        if (achievement == null)
            return 0;

        var newEntries = new List<AchievementEntry>();
        foreach (var user in users)
        {
            var entries = await achievementRepository.GetAllEntriesOfUser(user.Id);
            if (entries.All(e => e.Achievement.Id != achievementId))
            {
                newEntries.Add(new AchievementEntry
                {
                    Id = Guid.NewGuid(),
                    Achievement = achievement,
                    User = user,
                    HasSeen = false,
                    CreatedOn = DateTime.Now
                });
            }
        }
        if (newEntries.Any())
            await achievementRepository.CreateEntryRange(newEntries);
        return newEntries.Count;
    }
    
    private static bool CheckComparison(int actualValue, Achievement.ComparisonOperatorOption comparisonOperator, int targetValue)
    {
        return comparisonOperator switch
        {
            Achievement.ComparisonOperatorOption.LessThan => actualValue < targetValue,
            Achievement.ComparisonOperatorOption.GreaterThan => actualValue > targetValue,
            Achievement.ComparisonOperatorOption.LessThanOrEqual => actualValue <= targetValue,
            Achievement.ComparisonOperatorOption.GreaterThanOrEqual => actualValue >= targetValue,
            _ => false
        };
    }
}