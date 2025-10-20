using FluentAssertions;
using NSubstitute;
using SSSKLv2.Agents;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Test.Services;

[TestClass]
public class AchievementServiceTests
{
    private AchievementService _sut = null!;
    private IAchievementRepository _achievementRepository = null!;
    private IOrderRepository _orderRepository = null!;
    private ITopUpRepository _topUpRepository = null!;
    private IApplicationUserRepository _applicationUserRepository = null!;
    private IBlobStorageAgent _blobStorageAgent = null!;
    
    private ApplicationUser _testUser = null!;
    private Order _testOrder = null!;
    
    [TestInitialize]
    public void TestInitialize()
    {
        // Create mocks
        _achievementRepository = Substitute.For<IAchievementRepository>();
        _orderRepository = Substitute.For<IOrderRepository>();
        _topUpRepository = Substitute.For<ITopUpRepository>();
        _applicationUserRepository = Substitute.For<IApplicationUserRepository>();
        _blobStorageAgent = Substitute.For<IBlobStorageAgent>();
        
        // Create the system under test
        _sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository,
            _applicationUserRepository, _blobStorageAgent);
        
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
    
    #region CheckOrdersForAchievements Tests
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_EmptyOrdersList_ShouldNotThrow()
    {
        // Arrange
        var emptyOrders = new List<Order>();
        
        // Act
        await _sut.CheckOrdersForAchievements(emptyOrders);
        
        // Assert
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_NoUncompletedAchievements_ShouldNotCreateEntries()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement>());
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_AllAchievementsAutoAchieveFalse_ShouldNotCreateEntries()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievements = new List<Achievement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Manual Achievement",
                AutoAchieve = false,
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 1
            }
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(achievements);
        _orderRepository.GetPersonal(_testUser.Id).Returns(new List<Order> { _testOrder });
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_UserBuyAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Buy 5 Items",
            AutoAchieve = true,
            Action = Achievement.ActionOption.UserBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 5
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 2, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Amount = 3, CreatedOn = DateTime.Now.AddDays(-1) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id &&
                      entries.First().User.Id == _testUser.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_UserBuyAchievementNotMet_ShouldNotCreateEntry()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Buy 10 Items",
            AutoAchieve = true,
            Action = Achievement.ActionOption.UserBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 10
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 2, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Amount = 3, CreatedOn = DateTime.Now.AddDays(-1) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_TotalBuyAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Spend 50",
            AutoAchieve = true,
            Action = Achievement.ActionOption.TotalBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 50
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Paid = 20m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Paid = 35m, CreatedOn = DateTime.Now.AddDays(-1) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_TotalBuyWithDecimal_ShouldTruncateToInt()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Spend 50",
            AutoAchieve = true,
            Action = Achievement.ActionOption.TotalBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 50
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Paid = 49.99m, CreatedOn = DateTime.Now.AddDays(-5) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert - 49.99 truncates to 49, which is less than 50
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_UserTopUpAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Top Up 3 Times",
            AutoAchieve = true,
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
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_TotalTopUpAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Top Up Total 50",
            AutoAchieve = true,
            Action = Achievement.ActionOption.TotalTopUp,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 50
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var topUps = new List<TopUp>
        {
            new() { User = _testUser, Saldo = 20.4m, CreatedOn = DateTime.Now.AddDays(-10) },
            new() { User = _testUser, Saldo = 15.3m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Saldo = 15.3m, CreatedOn = DateTime.Now.AddDays(-1) }
            // Total: 51.0, rounds to 51
        };
        
        _topUpRepository.GetPersonal(_testUser.Id).Returns(topUps);
        _orderRepository.GetPersonal(_testUser.Id).Returns(new List<Order> { _testOrder });
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_TotalTopUpRoundsCorrectly_ShouldHandleRounding()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Top Up Total 50",
            AutoAchieve = true,
            Action = Achievement.ActionOption.TotalTopUp,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 50
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var topUps = new List<TopUp>
        {
            new() { User = _testUser, Saldo = 49.4m, CreatedOn = DateTime.Now.AddDays(-10) }
            // Total: 49.4, rounds to 49
        };
        
        _topUpRepository.GetPersonal(_testUser.Id).Returns(topUps);
        _orderRepository.GetPersonal(_testUser.Id).Returns(new List<Order> { _testOrder });
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert - 49.4 rounds to 49, which is less than 50
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_YearsOfMembershipAchievementMet_ShouldCreateEntry()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Member for 2 Years",
            AutoAchieve = true,
            Action = Achievement.ActionOption.YearsOfMembership,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 2
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, CreatedOn = DateTime.Now.AddYears(-3) }, // Oldest order from 3 years ago
            new() { User = _testUser, CreatedOn = DateTime.Now.AddYears(-1) },
            _testOrder
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Id == achievement.Id
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_YearsOfMembershipNoOldestOrder_ShouldNotCreateEntry()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Member for 2 Years",
            AutoAchieve = true,
            Action = Achievement.ActionOption.YearsOfMembership,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 2
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        // Empty orders history
        _orderRepository.GetPersonal(_testUser.Id).Returns(new List<Order>());
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_LessThanOperator_ShouldWorkCorrectly()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Less than 10 purchases",
            AutoAchieve = true,
            Action = Achievement.ActionOption.UserBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.LessThan,
            ComparisonValue = 10
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 5, CreatedOn = DateTime.Now.AddDays(-5) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert - 5 < 10 is true
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_GreaterThanOperator_ShouldWorkCorrectly()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "More than 2 purchases",
            AutoAchieve = true,
            Action = Achievement.ActionOption.UserBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThan,
            ComparisonValue = 2
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 5, CreatedOn = DateTime.Now.AddDays(-5) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert - 5 > 2 is true
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_LessThanOrEqualOperator_ShouldWorkCorrectly()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "At most 5 purchases",
            AutoAchieve = true,
            Action = Achievement.ActionOption.UserBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.LessThanOrEqual,
            ComparisonValue = 5
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 5, CreatedOn = DateTime.Now.AddDays(-5) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert - 5 <= 5 is true
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_MultipleAchievementsMet_ShouldCreateMultipleEntries()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievements = new List<Achievement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Buy 5 Items",
                AutoAchieve = true,
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 5
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Spend 50",
                AutoAchieve = true,
                Action = Achievement.ActionOption.TotalBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 50
            }
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(achievements);
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 3, Paid = 30m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Amount = 2, Paid = 25m, CreatedOn = DateTime.Now.AddDays(-1) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert - Both achievements should be awarded
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 2
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_MultipleOrders_ShouldProcessEachOrder()
    {
        // Arrange
        var user2 = new ApplicationUser
        {
            Id = "test-user-2",
            UserName = "testuser2",
            Name = "Test2",
            Surname = "User2"
        };
        
        var order1 = _testOrder;
        var order2 = new Order
        {
            Id = Guid.NewGuid(),
            User = user2,
            ProductNaam = "Test Product 2",
            Amount = 1,
            Paid = 10.0m,
            CreatedOn = DateTime.Now
        };
        
        var orders = new List<Order> { order1, order2 };
        
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Buy 5 Items",
            AutoAchieve = true,
            Action = Achievement.ActionOption.UserBuy,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 5
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(new List<Achievement> { achievement });
        _achievementRepository.GetUncompletedAchievementsForUser(user2.Id)
            .Returns(new List<Achievement> { achievement });
        
        var user1Orders = new List<Order>
        {
            new() { User = _testUser, Amount = 5, CreatedOn = DateTime.Now.AddDays(-5) }
        };
        var user2Orders = new List<Order>
        {
            new() { User = user2, Amount = 6, CreatedOn = DateTime.Now.AddDays(-5) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(user1Orders);
        _orderRepository.GetPersonal(user2.Id).Returns(user2Orders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert - Both users should get the achievement (called twice, once per user)
        await _achievementRepository.Received(2).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_MixedAutoAchieve_ShouldOnlyProcessAutoAchieve()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievements = new List<Achievement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Auto Achievement",
                AutoAchieve = true,
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 5
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Manual Achievement",
                AutoAchieve = false,
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 5
            }
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(achievements);
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 5, CreatedOn = DateTime.Now.AddDays(-5) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert - Only 1 achievement (the AutoAchieve one) should be awarded
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 1 && 
                      entries.First().Achievement.Name == "Auto Achievement"
        ));
    }
    
    [TestMethod]
    public async Task CheckOrdersForAchievements_AllActionTypesInSingleBatch_ShouldProcessAll()
    {
        // Arrange
        var orders = new List<Order> { _testOrder };
        var achievements = new List<Achievement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "UserBuy Achievement",
                AutoAchieve = true,
                Action = Achievement.ActionOption.UserBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 5
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "TotalBuy Achievement",
                AutoAchieve = true,
                Action = Achievement.ActionOption.TotalBuy,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 50
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "UserTopUp Achievement",
                AutoAchieve = true,
                Action = Achievement.ActionOption.UserTopUp,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 3
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "TotalTopUp Achievement",
                AutoAchieve = true,
                Action = Achievement.ActionOption.TotalTopUp,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 50
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "YearsOfMembership Achievement",
                AutoAchieve = true,
                Action = Achievement.ActionOption.YearsOfMembership,
                ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
                ComparisonValue = 2
            }
        };
        
        _achievementRepository.GetUncompletedAchievementsForUser(_testUser.Id)
            .Returns(achievements);
        
        var userOrders = new List<Order>
        {
            new() { User = _testUser, Amount = 3, Paid = 30m, CreatedOn = DateTime.Now.AddYears(-3) },
            new() { User = _testUser, Amount = 2, Paid = 25m, CreatedOn = DateTime.Now.AddDays(-1) }
        };
        
        var topUps = new List<TopUp>
        {
            new() { User = _testUser, Saldo = 20m, CreatedOn = DateTime.Now.AddDays(-10) },
            new() { User = _testUser, Saldo = 15m, CreatedOn = DateTime.Now.AddDays(-5) },
            new() { User = _testUser, Saldo = 16m, CreatedOn = DateTime.Now.AddDays(-1) }
        };
        
        _orderRepository.GetPersonal(_testUser.Id).Returns(userOrders);
        _topUpRepository.GetPersonal(_testUser.Id).Returns(topUps);
        
        // Act
        await _sut.CheckOrdersForAchievements(orders);
        
        // Assert - All 5 achievement types should be awarded
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(
            entries => entries.Count() == 5
        ));
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

    #region GetPersonalAchievements Tests

    [TestMethod]
    public async Task GetPersonalAchievements_NoAchievements_ReturnsEmptyList()
    {
        _achievementRepository.GetAll().Returns(new List<Achievement>());
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry>());
        var result = await _sut.GetPersonalAchievements(_testUser.Id);
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetPersonalAchievements_UserHasNoAchievements_ReturnsAllWithCompletedFalse()
    {
        var achievements = new List<Achievement> {
            new Achievement { Id = Guid.NewGuid(), Name = "A1" },
            new Achievement { Id = Guid.NewGuid(), Name = "A2" }
        };
        _achievementRepository.GetAll().Returns(achievements);
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry>());
        var result = await _sut.GetPersonalAchievements(_testUser.Id);
        result.Should().HaveCount(2);
        result.All(a => !a.Completed).Should().BeTrue();
    }

    [TestMethod]
    public async Task GetPersonalAchievements_UserHasSomeAchievements_ReturnsCorrectCompleted()
    {
        var a1 = new Achievement { Id = Guid.NewGuid(), Name = "A1" };
        var a2 = new Achievement { Id = Guid.NewGuid(), Name = "A2" };
        var achievements = new List<Achievement> { a1, a2 };
        var entries = new List<AchievementEntry> {
            new AchievementEntry { Achievement = a1, User = _testUser }
        };
        _achievementRepository.GetAll().Returns(achievements);
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(entries);
        var result = await _sut.GetPersonalAchievements(_testUser.Id);
        result.Should().HaveCount(2);
        result.Single(a => a.Name == "A1").Completed.Should().BeTrue();
        result.Single(a => a.Name == "A2").Completed.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetPersonalAchievements_UserHasAllAchievements_ReturnsAllCompletedTrue()
    {
        var a1 = new Achievement { Id = Guid.NewGuid(), Name = "A1" };
        var a2 = new Achievement { Id = Guid.NewGuid(), Name = "A2" };
        var achievements = new List<Achievement> { a1, a2 };
        var entries = new List<AchievementEntry> {
            new AchievementEntry { Achievement = a1, User = _testUser },
            new AchievementEntry { Achievement = a2, User = _testUser }
        };
        _achievementRepository.GetAll().Returns(achievements);
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(entries);
        var result = await _sut.GetPersonalAchievements(_testUser.Id);
        result.All(a => a.Completed).Should().BeTrue();
    }
    #endregion

    #region GetAchievements Tests

    [TestMethod]
    public async Task GetAchievements_NoAchievements_ReturnsEmpty()
    {
        _achievementRepository.GetAll().Returns(new List<Achievement>());
        var result = await _sut.GetAchievements();
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAchievements_MultipleAchievements_ReturnsAll()
    {
        var achievements = new List<Achievement> {
            new Achievement { Id = Guid.NewGuid(), Name = "A1" },
            new Achievement { Id = Guid.NewGuid(), Name = "A2" }
        };
        _achievementRepository.GetAll().Returns(achievements);
        var result = await _sut.GetAchievements();
        result.Should().BeEquivalentTo(achievements);
    }
    #endregion

    #region AwardAchievementToUser Tests

    [TestMethod]
    public async Task AwardAchievementToUser_AchievementDoesNotExist_ReturnsFalse()
    {
        _achievementRepository.GetAll().Returns(new List<Achievement>());
        var result = await _sut.AwardAchievementToUser(_testUser.Id, Guid.NewGuid());
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task AwardAchievementToUser_UserAlreadyHasAchievement_ReturnsFalse()
    {
        var achievementId = Guid.NewGuid();
        var achievement = new Achievement { Id = achievementId, Name = "A1" };
        var entry = new AchievementEntry { Achievement = achievement, User = _testUser };
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry> { entry });
        var result = await _sut.AwardAchievementToUser(_testUser.Id, achievementId);
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task AwardAchievementToUser_UserDoesNotHaveAchievement_AwardsAndReturnsTrue()
    {
        var achievementId = Guid.NewGuid();
        var achievement = new Achievement { Id = achievementId, Name = "A1" };
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry>());
        _achievementRepository.CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>()).Returns(Task.CompletedTask);
        var result = await _sut.AwardAchievementToUser(_testUser.Id, achievementId);
        result.Should().BeTrue();
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(entries =>
            entries.Count() == 1 && entries.First().Achievement.Id == achievementId));
    }
    #endregion

    #region AwardAchievementToAllUsers Tests

    [TestMethod]
    public async Task AwardAchievementToAllUsers_AchievementDoesNotExist_ReturnsZero()
    {
        var applicationUserRepository = Substitute.For<IApplicationUserRepository>();
        var sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository, applicationUserRepository, _blobStorageAgent);
        applicationUserRepository.GetAll().Returns(new List<ApplicationUser>());
        _achievementRepository.GetAll().Returns(new List<Achievement>());
        var result = await sut.AwardAchievementToAllUsers(Guid.NewGuid());
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task AwardAchievementToAllUsers_NoUsers_ReturnsZero()
    {
        var achievementId = Guid.NewGuid();
        var achievement = new Achievement { Id = achievementId, Name = "A1" };
        var applicationUserRepository = Substitute.For<IApplicationUserRepository>();
        var sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository, applicationUserRepository, _blobStorageAgent);
        applicationUserRepository.GetAll().Returns(new List<ApplicationUser>());
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        var result = await sut.AwardAchievementToAllUsers(achievementId);
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task AwardAchievementToAllUsers_AllUsersAlreadyHaveAchievement_ReturnsZero()
    {
        var achievementId = Guid.NewGuid();
        var achievement = new Achievement { Id = achievementId, Name = "A1" };
        var users = new List<ApplicationUser> { _testUser };
        var entry = new AchievementEntry { Achievement = achievement, User = _testUser };
        var applicationUserRepository = Substitute.For<IApplicationUserRepository>();
        var sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository, applicationUserRepository, _blobStorageAgent);
        applicationUserRepository.GetAll().Returns(users);
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry> { entry });
        var result = await sut.AwardAchievementToAllUsers(achievementId);
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task AwardAchievementToAllUsers_SomeUsersDoNotHaveAchievement_AwardsAndReturnsCount()
    {
        var achievementId = Guid.NewGuid();
        var achievement = new Achievement { Id = achievementId, Name = "A1" };
        var user1 = new ApplicationUser { Id = "u1" };
        var user2 = new ApplicationUser { Id = "u2" };
        var users = new List<ApplicationUser> { user1, user2 };
        var applicationUserRepository = Substitute.For<IApplicationUserRepository>();
        var sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository, applicationUserRepository, _blobStorageAgent);
        applicationUserRepository.GetAll().Returns(users);
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        _achievementRepository.GetAllEntriesOfUser("u1").Returns(new List<AchievementEntry>());
        _achievementRepository.GetAllEntriesOfUser("u2").Returns(new List<AchievementEntry> { new AchievementEntry { Achievement = achievement, User = user2 } });
        _achievementRepository.CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>()).Returns(Task.CompletedTask);
        var result = await sut.AwardAchievementToAllUsers(achievementId);
        result.Should().Be(1);
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(entries => entries.Count() == 1 && entries.First().User.Id == "u1"));
    }
    #endregion
}