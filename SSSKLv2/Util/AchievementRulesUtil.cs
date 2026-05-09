using SSSKLv2.Data;

namespace SSSKLv2.Util;

public class AchievementRulesUtil
{
    public static bool CheckSpecialAchievementRules(Achievement achievement, ApplicationUser user)
    {
        // This can remain as a fallback or for rules that strictly need the full User object
        // But per request, we should favor UserStat
        return false;
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

    public static bool CheckStatAchievement(Achievement achievement, UserStat stats)
    {
        int actualValue = achievement.Action switch
        {
            Achievement.ActionOption.UserOrderAmountBought => stats.TotalItemsBought,
            Achievement.ActionOption.UserOrderAmountPaid => (int)stats.TotalSpent,
            Achievement.ActionOption.UserIndividualTopUp => (int)stats.MaxSingleTopUp,
            Achievement.ActionOption.UserTotalTopUp => (int)stats.TotalTopUp,
            Achievement.ActionOption.QuoteCount => stats.QuoteCount,
            Achievement.ActionOption.QuoteVotesReceived => stats.QuoteVotesReceived,
            Achievement.ActionOption.ReactionCount => stats.ReactionCount,
            Achievement.ActionOption.CurrentStreak => stats.CurrentStreak,
            Achievement.ActionOption.OrdersWithinHour => stats.MaxOrdersPerHour,
            Achievement.ActionOption.MinutesBetweenOrders => stats.MinMinutesBetweenOrders,
            Achievement.ActionOption.MinutesBetweenTopUp => stats.MinMinutesBetweenTopUp,
            Achievement.ActionOption.YearsOfMembership => stats.MembershipStartDate.HasValue 
                ? (DateTime.UtcNow - stats.MembershipStartDate.Value).Days / 365 
                : 0,
            _ => 0
        };

        return CheckComparison(actualValue, achievement.ComparisonOperator, achievement.ComparisonValue);
    }
}