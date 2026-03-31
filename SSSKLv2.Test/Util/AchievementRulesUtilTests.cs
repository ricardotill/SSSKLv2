using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Util;

namespace SSSKLv2.Test.Util;

[TestClass]
public class AchievementRulesUtilTests
{
    [TestMethod]
    [DataRow(5, Achievement.ComparisonOperatorOption.GreaterThan, 3, true)]
    [DataRow(5, Achievement.ComparisonOperatorOption.GreaterThan, 5, false)]
    [DataRow(5, Achievement.ComparisonOperatorOption.LessThan, 10, true)]
    [DataRow(5, Achievement.ComparisonOperatorOption.LessThan, 5, false)]
    [DataRow(5, Achievement.ComparisonOperatorOption.GreaterThanOrEqual, 5, true)]
    [DataRow(5, Achievement.ComparisonOperatorOption.LessThanOrEqual, 5, true)]
    [DataRow(5, Achievement.ComparisonOperatorOption.None, 5, false)]
    public void CheckComparison_HandlesOperatorsCorrectly(int actual, Achievement.ComparisonOperatorOption op, int target, bool expected)
    {
        var result = AchievementRulesUtil.CheckComparison(actual, op, target);
        result.Should().Be(expected);
    }

    [TestMethod]
    public void CheckSpecialAchievementRules_YearsOfMembership_AwardsWhenCriteriaMet()
    {
        // Arrange
        var achievement = new Achievement
        {
            Action = Achievement.ActionOption.YearsOfMembership,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 2
        };
        var user = new ApplicationUser
        {
            Orders = new List<Order>
            {
                new Order { CreatedOn = DateTime.Now.AddYears(-3) }
            }
        };

        // Act
        var result = AchievementRulesUtil.CheckSpecialAchievementRules(achievement, user);

        // Assert
        result.Should().BeTrue();
    }

    [TestMethod]
    public void CheckSpecialAchievementRules_YearsOfMembership_DoesNotAwardWhenCriteriaNotMet()
    {
        // Arrange
        var achievement = new Achievement
        {
            Action = Achievement.ActionOption.YearsOfMembership,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 5
        };
        var user = new ApplicationUser
        {
            Orders = new List<Order>
            {
                new Order { CreatedOn = DateTime.Now.AddYears(-1) }
            }
        };

        // Act
        var result = AchievementRulesUtil.CheckSpecialAchievementRules(achievement, user);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void CheckSpecialAchievementRules_YearsOfMembership_NoOrders_ReturnsFalse()
    {
        // Arrange
        var achievement = new Achievement { Action = Achievement.ActionOption.YearsOfMembership };
        var user = new ApplicationUser { Orders = new List<Order>() };

        // Act
        var result = AchievementRulesUtil.CheckSpecialAchievementRules(achievement, user);

        // Assert
        result.Should().BeFalse();
    }

    [TestMethod]
    public void CheckSpecialAchievementRules_UnknownAction_ReturnsFalse()
    {
        // Arrange
        var achievement = new Achievement { Action = Achievement.ActionOption.None };
        var user = new ApplicationUser();

        // Act
        var result = AchievementRulesUtil.CheckSpecialAchievementRules(achievement, user);

        // Assert
        result.Should().BeFalse();
    }
}
