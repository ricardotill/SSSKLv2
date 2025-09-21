using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class AchievementService(
    IAchievementRepository achievementRepository,
    IOrderRepository orderRepository,
    ITopUpRepository topUpRepository) : IAchievementService
{
    // TODO: Add function to award achievement for a single user
    // TODO: Add function to award achievement to all users
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