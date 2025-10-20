using SSSKLv2.Data;

namespace SSSKLv2.Util;

public class AchievementRulesUtil
{
    public static bool CheckSpecialAchievementRules(Achievement achievement, ApplicationUser user)
    {
        bool shouldAward = false;
        switch (achievement.Action)
        {
            case Achievement.ActionOption.YearsOfMembership:
                // Calculate years of membership based on the oldest order date as a proxy for membership start
                var oldestOrder = user.Orders.OrderBy(o => o.CreatedOn).FirstOrDefault();
                if (oldestOrder != null)
                {
                    var membershipYears = (DateTime.Now - oldestOrder.CreatedOn).Days / 365;
                    shouldAward = CheckComparison(membershipYears, achievement.ComparisonOperator, achievement.ComparisonValue);
                }
                break;
        }

        return shouldAward;
    }
    
    public static bool CheckComparison(int actualValue, Achievement.ComparisonOperatorOption comparisonOperator, int targetValue)
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