using FluentAssertions;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;

namespace SSSKLv2.Test.Services;

[TestClass]
public class AchievementServiceTests
{
    private AchievementService _sut = null!;
    private IAchievementRepository _achievementRepository = null!;
    private IOrderRepository _orderRepository = null!;
    private ITopUpRepository _topUpRepository = null!;
    
    private ApplicationUser _testUser = null!;
    private Order _testOrder = null!;
    
    [TestInitialize]
    public void TestInitialize()
    {
        // Create mocks
        _achievementRepository = Substitute.For<IAchievementRepository>();
        _orderRepository = Substitute.For<IOrderRepository>();
        _topUpRepository = Substitute.For<ITopUpRepository>();
        
        // Create the system under test
        _sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository);
        
        // Create test user
        _testUser = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Name = "Test",
            Surname = "User",
            Email = "test@example.com"
        };
        
        // Create test order
        _testOrder = new Order
        {
            Id = Guid.NewGuid(),
            User = _testUser,
            ProductNaam = "Test Product",
            Amount = 1,
            Paid = 10.0m,
            CreatedOn = DateTime.Now
        };
    }
    
    #region CheckOrderForAchievements Tests
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenNoUncompletedAchievements_ShouldNotCreateEntries()
    {
        // Arrange
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement>());
            
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenUserBuyAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Buy 5 Items",
            Action = Achievement.ActionOption.UserBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 5
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
            
        var pastOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 2, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Amount = 3, CreatedOn = DateTime.Now.AddDays(-1) },
            _testOrder // Current order with Amount = 1, total = 6
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(pastOrders);
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id &&
                      entries.First().User.Id == _testUser.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenUserBuyAchievementNotMet_ShouldNotCreateEntry()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Buy 10 Items",
            Action = Achievement.ActionOption.UserBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 10
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
            
        var pastOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 2, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Amount = 3, CreatedOn = DateTime.Now.AddDays(-1) },
            _testOrder // Current order with Amount = 1, total = 6
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(pastOrders);
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenTotalBuyAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Spend 50",
            Action = Achievement.ActionOption.TotalBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 50
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
            
        var pastOrders = new List<Order>
        {
            new() { User = _testUser, Paid = 20m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Paid = 25m, CreatedOn = DateTime.Now.AddDays(-1) },
            _testOrder // Current order with Paid = 10, total = 55
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(pastOrders);
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id &&
                      entries.First().User.Id == _testUser.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenUserTopUpAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Top Up 3 Times",
            Action = Achievement.ActionOption.UserTopUp,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 3
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
            
        var topUps = new List<TopUp>
        {
            new() { User = _testUser, Saldo = 10m, CreatedOn = DateTime.Now.AddDays(-10) },
            new() { User = _testUser, Saldo = 20m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Saldo = 15m, CreatedOn = DateTime.Now.AddDays(-1) }
        };
        
        _topUpRepository.GetPersonal(_testUser.Id).Returns(topUps);
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(new List<Order> { _testOrder });
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id &&
                      entries.First().User.Id == _testUser.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenTotalTopUpAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Top Up Total 50",
            Action = Achievement.ActionOption.TotalTopUp,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 50
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
            
        var topUps = new List<TopUp>
        {
            new() { User = _testUser, Saldo = 20m, CreatedOn = DateTime.Now.AddDays(-10) },
            new() { User = _testUser, Saldo = 15.5m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Saldo = 15m, CreatedOn = DateTime.Now.AddDays(-1) }
            // Total: 50.5, rounds to 51
        };
        
        _topUpRepository.GetPersonal(_testUser.Id).Returns(topUps);
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(new List<Order> { _testOrder });
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id &&
                      entries.First().User.Id == _testUser.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenYearsOfMembershipAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Member for 2 Years",
            Action = Achievement.ActionOption.YearsOfMembership,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 2
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
            
        var pastOrders = new List<Order>
        {
            new() { User = _testUser, CreatedOn = DateTime.Now.AddYears(-3) }, // Oldest order from 3 years ago
            new() { User = _testUser, CreatedOn = DateTime.Now.AddYears(-1) },
            _testOrder // Current order
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(pastOrders);
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id &&
                      entries.First().User.Id == _testUser.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenYearsOfMembershipNoOldOrders_ShouldNotCreateEntry()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Member for 2 Years",
            Action = Achievement.ActionOption.YearsOfMembership,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 2
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
            
        // Only the current order exists
        _orderRepository.GetPersonal(_testUser.Id).Returns(new List<Order> { _testOrder });
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WithDifferentComparisonOperators_ShouldApplyCorrectly()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Less than 10 purchases",
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.LessThan,
                ComparisonValue = 10
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Greater than 2 purchases",
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThan,
                ComparisonValue = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Exactly 5 purchases (LE)",
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.LessThanOrEqual,
                ComparisonValue = 5
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "At least 5 purchases (GE)",
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 5
            }
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(achievements);
            
        var pastOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 1, CreatedOn = DateTime.Now.AddDays(-10) },
            new() { User = _testUser, Amount = 1, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Amount = 2, CreatedOn = DateTime.Now.AddDays(-1) },
            _testOrder // Current order with Amount = 1, total = 5
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(pastOrders);
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert - All 4 achievements should be awarded
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 4
        ));
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WithMultipleAchievementsMet_ShouldCreateMultipleEntries()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Buy 5 Items",
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 5
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Spend 50",
                Action = Achievement.ActionOption.TotalBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 50
            }
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(achievements);
            
        var pastOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 2, Paid = 20m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Amount = 3, Paid = 25m, CreatedOn = DateTime.Now.AddDays(-1) },
            _testOrder // Current order with Amount = 1, Paid = 10m, totals = 6 items, 55m spent
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(pastOrders);
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert - Both achievements should be awarded
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 2
        ));
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenAllAchievementTypesMet_ShouldCreateAllEntries()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Buy 5 Items",
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 5
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Spend 50",
                Action = Achievement.ActionOption.TotalBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 50
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Top Up 3 Times",
                Action = Achievement.ActionOption.UserTopUp,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 3
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Top Up Total 50",
                Action = Achievement.ActionOption.TotalTopUp,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 50
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Member for 2 Years",
                Action = Achievement.ActionOption.YearsOfMembership,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 2
            }
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(achievements);
            
        var pastOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 2, Paid = 20m, CreatedOn = DateTime.Now.AddYears(-3) },
            new() { User = _testUser, Amount = 2, Paid = 25m, CreatedOn = DateTime.Now.AddDays(-1) },
            _testOrder // Current order with Amount = 1, Paid = 10m, totals = 5 items, 55m spent
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(pastOrders);
        
        var topUps = new List<TopUp>
        {
            new() { User = _testUser, Saldo = 20m, CreatedOn = DateTime.Now.AddDays(-10) },
            new() { User = _testUser, Saldo = 15m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Saldo = 15m, CreatedOn = DateTime.Now.AddDays(-1) }
        };
        
        _topUpRepository.GetPersonal(_testUser.Id).Returns(topUps);
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert - All 5 achievements should be awarded
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 5
        ));
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenNoAchievementsMet_ShouldNotCreateEntries()
    {
        // Arrange
        var achievements = new List<Achievement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Buy 100 Items",
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 100
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Spend 500",
                Action = Achievement.ActionOption.TotalBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 500
            }
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(achievements);
            
        var pastOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 2, Paid = 20m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Amount = 3, Paid = 25m, CreatedOn = DateTime.Now.AddDays(-1) },
            _testOrder // Current order with Amount = 1, Paid = 10m, totals = 6 items, 55m spent
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(pastOrders);
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert - No achievements should be awarded
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrderForAchievements_WhenInvalidComparisonOperator_ShouldNotCreateEntries()
    {
        // Arrange
        var invalidAchievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Invalid Operator",
            Action = Achievement.ActionOption.UserBuy,
            ComparisonOperator = (Achievement.ComparisonOperatorOption)999, // Invalid enum value
            ComparisonValue = 5
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { invalidAchievement });
            
        var pastOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 2, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Amount = 3, CreatedOn = DateTime.Now.AddDays(-1) },
            _testOrder // Current order with Amount = 1, total = 6
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(pastOrders);
        
        // Act
        await _sut.CheckOrderForAchievements(_testOrder);
        
        // Assert - No achievements should be awarded due to invalid operator
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    #endregion
    
    #region CheckComparison Tests
    
    [TestMethod]
    public void CheckComparison_LessThan_ReturnsCorrectResult()
    {
        // Act & Assert
        CheckComparisonMethod(5, Achievement.ComparisonOperatorOption.LessThan, 10, true);
        CheckComparisonMethod(10, Achievement.ComparisonOperatorOption.LessThan, 10, false);
        CheckComparisonMethod(15, Achievement.ComparisonOperatorOption.LessThan, 10, false);
    }
    
    [TestMethod]
    public void CheckComparison_GreaterThan_ReturnsCorrectResult()
    {
        // Act & Assert
        CheckComparisonMethod(15, Achievement.ComparisonOperatorOption.GreaterThan, 10, true);
        CheckComparisonMethod(10, Achievement.ComparisonOperatorOption.GreaterThan, 10, false);
        CheckComparisonMethod(5, Achievement.ComparisonOperatorOption.GreaterThan, 10, false);
    }
    
    [TestMethod]
    public void CheckComparison_LessThanOrEqual_ReturnsCorrectResult()
    {
        // Act & Assert
        CheckComparisonMethod(5, Achievement.ComparisonOperatorOption.LessThanOrEqual, 10, true);
        CheckComparisonMethod(10, Achievement.ComparisonOperatorOption.LessThanOrEqual, 10, true);
        CheckComparisonMethod(15, Achievement.ComparisonOperatorOption.LessThanOrEqual, 10, false);
    }
    
    [TestMethod]
    public void CheckComparison_GreaterThanOrEqual_ReturnsCorrectResult()
    {
        // Act & Assert
        CheckComparisonMethod(15, Achievement.ComparisonOperatorOption.GreaterThanOrEqual, 10, true);
        CheckComparisonMethod(10, Achievement.ComparisonOperatorOption.GreaterThanOrEqual, 10, true);
        CheckComparisonMethod(5, Achievement.ComparisonOperatorOption.GreaterThanOrEqual, 10, false);
    }
    
    [TestMethod]
    public void CheckComparison_InvalidOperator_ReturnsFalse()
    {
        // Act & Assert
        CheckComparisonMethod(10, (Achievement.ComparisonOperatorOption)999, 10, false);
    }
    
    private static void CheckComparisonMethod(int actual, Achievement.ComparisonOperatorOption op, int target, bool expected)
    {
        // Using reflection to access private static method
        var methodInfo = typeof(AchievementService).GetMethod("CheckComparison", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
        var result = methodInfo?.Invoke(null, new object[] { actual, op, target });
        result.Should().Be(expected);
    }
    
    #endregion
}