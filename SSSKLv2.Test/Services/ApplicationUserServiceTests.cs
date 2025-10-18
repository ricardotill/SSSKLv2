using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Components;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSSKLv2.Test.Services;

[TestClass]
public class ApplicationUserServiceTests
{
    private IApplicationUserRepository _mockUserRepository = null!;
    private IProductRepository _mockProductRepository = null!;
    private ILogger<ApplicationUserService> _mockLogger = null!;
    private ApplicationUserService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockUserRepository = Substitute.For<IApplicationUserRepository>();
        _mockProductRepository = Substitute.For<IProductRepository>();
        _mockLogger = Substitute.For<ILogger<ApplicationUserService>>();
        _sut = new ApplicationUserService(_mockUserRepository, _mockProductRepository, _mockLogger);
    }

    #region GetUserById Tests

    [TestMethod]
    public async Task GetUserById_WithValidId_ReturnsUser()
    {
        // Arrange
        var userId = "user-123";
        var expectedUser = CreateApplicationUser(userId);
        _mockUserRepository.GetById(userId).Returns(expectedUser);

        // Act
        var result = await _sut.GetUserById(userId);

        // Assert
        result.Should().BeEquivalentTo(expectedUser);
        await _mockUserRepository.Received(1).GetById(userId);
    }

    [TestMethod]
    public async Task GetUserById_WithInvalidId_PropagatesNotFoundException()
    {
        // Arrange
        var userId = "non-existent-id";
        _mockUserRepository.GetById(userId).Returns(Task.FromException<ApplicationUser>(new NotFoundException("User not found")));

        // Act
        Func<Task> action = async () => await _sut.GetUserById(userId);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("User not found");
        await _mockUserRepository.Received(1).GetById(userId);
    }

    #endregion

    #region GetUserByUsername Tests

    [TestMethod]
    public async Task GetUserByUsername_WithValidUsername_ReturnsUser()
    {
        // Arrange
        var username = "testuser";
        var expectedUser = CreateApplicationUser("user-123", username);
        _mockUserRepository.GetByUsername(username).Returns(expectedUser);

        // Act
        var result = await _sut.GetUserByUsername(username);

        // Assert
        result.Should().BeEquivalentTo(expectedUser);
        await _mockUserRepository.Received(1).GetByUsername(username);
    }

    [TestMethod]
    public async Task GetUserByUsername_WithInvalidUsername_PropagatesNotFoundException()
    {
        // Arrange
        var username = "non-existent-user";
        _mockUserRepository.GetByUsername(username).Returns(Task.FromException<ApplicationUser>(new NotFoundException("User not found")));

        // Act
        Func<Task> action = async () => await _sut.GetUserByUsername(username);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("User not found");
        await _mockUserRepository.Received(1).GetByUsername(username);
    }

    #endregion

    #region GetAllUsers Tests

    [TestMethod]
    public async Task GetAllUsers_ReturnsAllUsersFromRepository()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            CreateApplicationUser("user-1", "user1"),
            CreateApplicationUser("user-2", "user2")
        };
        _mockUserRepository.GetAll().Returns(users);

        // Act
        var result = await _sut.GetAllUsers();

        // Assert
        result.Should().BeEquivalentTo(users);
        await _mockUserRepository.Received(1).GetAll();
    }

    [TestMethod]
    public async Task GetAllUsers_WhenRepositoryReturnsEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = new List<ApplicationUser>();
        _mockUserRepository.GetAll().Returns(emptyList);

        // Act
        var result = await _sut.GetAllUsers();

        // Assert
        result.Should().BeEmpty();
        await _mockUserRepository.Received(1).GetAll();
    }

    #endregion

    #region GetAllUsersObscured Tests

    [TestMethod]
    public async Task GetAllUsersObscured_ReturnsObscuredUserList()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            CreateApplicationUser("user-1", "user1", "Test1", "User1", "test1@example.com"),
            CreateApplicationUser("user-2", "user2", "Test2", "User2", "test2@example.com")
        };
        _mockUserRepository.GetAllForAdmin().Returns(users);

        // Act
        var result = await _sut.GetAllUsersObscured();
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(2);
        resultList.Should().AllSatisfy(u => u.PasswordHash.Should().Be("*****"));
        
        // Verify specific user properties were retained
        resultList[0].Id.Should().Be("user-1");
        resultList[0].UserName.Should().Be("user1");
        resultList[0].Name.Should().Be("Test1");
        resultList[0].Surname.Should().Be("User1");
        resultList[0].Email.Should().Be("test1@example.com");
        
        await _mockUserRepository.Received(1).GetAllForAdmin();
    }

    [TestMethod]
    public async Task GetAllUsersObscured_WhenRepositoryReturnsEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = new List<ApplicationUser>();
        _mockUserRepository.GetAllForAdmin().Returns(emptyList);

        // Act
        var result = await _sut.GetAllUsersObscured();

        // Assert
        result.Should().BeEmpty();
        await _mockUserRepository.Received(1).GetAllForAdmin();
    }

    #endregion

    #region GetAllLeaderboard Tests

    [TestMethod]
    public async Task GetAllLeaderboard_WithValidProductId_ReturnsLeaderboard()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        var users = CreateUsersWithOrders(product);
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetAllWithOrders().Returns(users);

        // Act
        var result = await _sut.GetAllLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(2); // Only users with orders for this product should be included
        resultList[0].Position.Should().Be(1);
        resultList[0].Amount.Should().Be(5); // User1 has 5 orders for this product
        resultList[0].ProductName.Should().Be("Test Product");
        resultList[0].FullName.Should().Be("Test1 U"); // Name + first letter of surname
        
        resultList[1].Position.Should().Be(2);
        resultList[1].Amount.Should().Be(3); // User2 has 3 orders for this product
        
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetAllWithOrders();
    }

    [TestMethod]
    public async Task GetAllLeaderboard_WithOverflowAmount_HandlesOverflow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        
        // Create a user with an order amount that would cause overflow when summed
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        var overflowOrder = new Order 
        { 
            Id = Guid.NewGuid(), 
            Product = product,
            Amount = int.MaxValue,
            CreatedOn = DateTime.Now 
        };
        
        var secondOverflowOrder = new Order 
        { 
            Id = Guid.NewGuid(), 
            Product = product,
            Amount = int.MaxValue,
            CreatedOn = DateTime.Now 
        };
        
        user.Orders = new List<Order> { overflowOrder, secondOverflowOrder };
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetAllWithOrders().Returns(new List<ApplicationUser> { user });

        // Act
        var result = await _sut.GetAllLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(1);
        resultList[0].Amount.Should().Be(int.MaxValue); // Amount should be capped at int.MaxValue
        resultList[0].Position.Should().Be(1);
        
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetAllWithOrders();
    }

    [TestMethod]
    public async Task GetAllLeaderboard_WithNoQualifyingOrders_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        var differentProduct = CreateProduct(Guid.NewGuid(), "Different Product");
        
        // Create users with orders for a different product
        var users = new List<ApplicationUser>();
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        user.Orders = new List<Order> 
        { 
            new Order 
            { 
                Id = Guid.NewGuid(), 
                Product = differentProduct, 
                Amount = 5,
                CreatedOn = DateTime.Now 
            }
        };
        users.Add(user);
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetAllWithOrders().Returns(users);

        // Act
        var result = await _sut.GetAllLeaderboard(productId);

        // Assert
        result.Should().BeEmpty();
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetAllWithOrders();
    }

    #endregion

    #region GetMonthlyLeaderboard Tests

    [TestMethod]
    public async Task GetMonthlyLeaderboard_WithValidProductId_ReturnsMonthlyLeaderboard()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        
        // Create users with orders in current month and previous month
        var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 15);
        var previousMonth = currentMonth.AddMonths(-1);
        
        var users = new List<ApplicationUser>();
        
        var user1 = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        user1.Orders = new List<Order> 
        { 
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 5, CreatedOn = currentMonth },
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 10, CreatedOn = previousMonth } // This should be excluded
        };
        users.Add(user1);
        
        var user2 = CreateApplicationUser("user-2", "user2", "Test2", "User2");
        user2.Orders = new List<Order> 
        { 
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 3, CreatedOn = currentMonth }
        };
        users.Add(user2);
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetAllWithOrders().Returns(users);

        // Act
        var result = await _sut.GetMonthlyLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(2);
        resultList[0].Position.Should().Be(1);
        resultList[0].Amount.Should().Be(5); // Only current month orders should be counted
        resultList[1].Position.Should().Be(2);
        resultList[1].Amount.Should().Be(3);
        
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetAllWithOrders();
    }

    [TestMethod]
    public async Task GetMonthlyLeaderboard_WithOverflowAmount_HandlesOverflow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 15);
        
        // Create a user with an order amount that would cause overflow when summed
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        var overflowOrder = new Order 
        { 
            Id = Guid.NewGuid(), 
            Product = product,
            Amount = int.MaxValue,
            CreatedOn = currentMonth
        };
        
        var secondOverflowOrder = new Order 
        { 
            Id = Guid.NewGuid(), 
            Product = product,
            Amount = int.MaxValue,
            CreatedOn = currentMonth
        };
        
        user.Orders = new List<Order> { overflowOrder, secondOverflowOrder };
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetAllWithOrders().Returns(new List<ApplicationUser> { user });

        // Act
        var result = await _sut.GetMonthlyLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(1);
        resultList[0].Amount.Should().Be(int.MaxValue); // Amount should be capped at int.MaxValue
        resultList[0].Position.Should().Be(1);
        
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetAllWithOrders();
    }

    [TestMethod]
    public async Task GetMonthlyLeaderboard_WithNoQualifyingOrders_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        
        // Create users with orders in previous month only
        var previousMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
        
        var users = new List<ApplicationUser>();
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        user.Orders = new List<Order> 
        { 
            new Order 
            { 
                Id = Guid.NewGuid(), 
                Product = product, 
                Amount = 5,
                CreatedOn = previousMonth // Previous month order
            }
        };
        users.Add(user);
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetAllWithOrders().Returns(users);

        // Act
        var result = await _sut.GetMonthlyLeaderboard(productId);

        // Assert
        result.Should().BeEmpty();
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetAllWithOrders();
    }

    #endregion

    #region Get12HourlyLeaderboard Tests

    [TestMethod]
    public async Task Get12HourlyLeaderboard_WithValidProductId_Returns12HourLeaderboard()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        
        // Create users with orders within and before 12-hour window
        var withinWindow = DateTime.Now.AddHours(-6);
        var beforeWindow = DateTime.Now.AddHours(-14);
        
        var users = new List<ApplicationUser>();
        
        var user1 = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        user1.Orders = new List<Order> 
        { 
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 5, CreatedOn = withinWindow },
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 10, CreatedOn = beforeWindow } // This should be excluded
        };
        users.Add(user1);
        
        var user2 = CreateApplicationUser("user-2", "user2", "Test2", "User2");
        user2.Orders = new List<Order> 
        { 
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 8, CreatedOn = withinWindow }
        };
        users.Add(user2);
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetAllWithOrders().Returns(users);

        // Act
        var result = await _sut.Get12HourlyLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(2);
        resultList[0].Position.Should().Be(1);
        resultList[0].Amount.Should().Be(8); // User2 has more orders in the last 12 hours
        resultList[1].Position.Should().Be(2);
        resultList[1].Amount.Should().Be(5); // User1 has 5 orders in the last 12 hours
        
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetAllWithOrders();
    }

    [TestMethod]
    public async Task Get12HourlyLeaderboard_WithOverflowAmount_HandlesOverflow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        var withinWindow = DateTime.Now.AddHours(-6);
        
        // Create a user with an order amount that would cause overflow when summed
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        var overflowOrder = new Order 
        { 
            Id = Guid.NewGuid(), 
            Product = product,
            Amount = int.MaxValue,
            CreatedOn = withinWindow
        };
        
        var secondOverflowOrder = new Order 
        { 
            Id = Guid.NewGuid(), 
            Product = product,
            Amount = int.MaxValue,
            CreatedOn = withinWindow
        };
        
        user.Orders = new List<Order> { overflowOrder, secondOverflowOrder };
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetAllWithOrders().Returns(new List<ApplicationUser> { user });

        // Act
        var result = await _sut.Get12HourlyLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(1);
        resultList[0].Amount.Should().Be(int.MaxValue); // Amount should be capped at int.MaxValue
        resultList[0].Position.Should().Be(1);
        
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetAllWithOrders();
    }

    [TestMethod]
    public async Task Get12HourlyLeaderboard_WithNoQualifyingOrders_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        
        // Create users with orders older than 12 hours
        var beforeWindow = DateTime.Now.AddHours(-14);
        
        var users = new List<ApplicationUser>();
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        user.Orders = new List<Order> 
        { 
            new Order 
            { 
                Id = Guid.NewGuid(), 
                Product = product, 
                Amount = 5,
                CreatedOn = beforeWindow // Order outside 12-hour window
            }
        };
        users.Add(user);
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetAllWithOrders().Returns(users);

        // Act
        var result = await _sut.Get12HourlyLeaderboard(productId);

        // Assert
        result.Should().BeEmpty();
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetAllWithOrders();
    }

    #endregion

    #region Get12HourlyLiveLeaderboard Tests

    [TestMethod]
    public async Task Get12HourlyLiveLeaderboard_WithValidProductId_Returns12HourLiveLeaderboard()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        
        // Create users with orders within and before 12-hour window
        var withinWindow = DateTime.Now.AddHours(-6);
        var beforeWindow = DateTime.Now.AddHours(-14);
        
        var users = new List<ApplicationUser>();
        
        var user1 = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        user1.Orders = new List<Order> 
        { 
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 5, CreatedOn = withinWindow },
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 10, CreatedOn = beforeWindow } // This should be excluded
        };
        users.Add(user1);
        
        var user2 = CreateApplicationUser("user-2", "user2", "Test2", "User2");
        user2.Orders = new List<Order> 
        { 
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 8, CreatedOn = withinWindow }
        };
        users.Add(user2);
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetFirst12WithOrders().Returns(users);

        // Act
        var result = await _sut.Get12HourlyLiveLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(2);
        resultList[0].Position.Should().Be(1);
        resultList[0].Amount.Should().Be(8); // User2 has more orders in the last 12 hours
        resultList[1].Position.Should().Be(2);
        resultList[1].Amount.Should().Be(5); // User1 has 5 orders in the last 12 hours
        
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetFirst12WithOrders();
    }

    [TestMethod]
    public async Task Get12HourlyLiveLeaderboard_WithOverflowAmount_HandlesOverflow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        var withinWindow = DateTime.Now.AddHours(-6);
        
        // Create a user with an order amount that would cause overflow when summed
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        var overflowOrder = new Order 
        { 
            Id = Guid.NewGuid(), 
            Product = product,
            Amount = int.MaxValue,
            CreatedOn = withinWindow
        };
        
        var secondOverflowOrder = new Order 
        { 
            Id = Guid.NewGuid(), 
            Product = product,
            Amount = int.MaxValue,
            CreatedOn = withinWindow
        };
        
        user.Orders = new List<Order> { overflowOrder, secondOverflowOrder };
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetFirst12WithOrders().Returns(new List<ApplicationUser> { user });

        // Act
        var result = await _sut.Get12HourlyLiveLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(1);
        resultList[0].Amount.Should().Be(int.MaxValue); // Amount should be capped at int.MaxValue
        resultList[0].Position.Should().Be(1);
        
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetFirst12WithOrders();
    }

    [TestMethod]
    public async Task Get12HourlyLiveLeaderboard_WithNoQualifyingOrders_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        
        // Create users with orders older than 12 hours
        var beforeWindow = DateTime.Now.AddHours(-14);
        
        var users = new List<ApplicationUser>();
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        user.Orders = new List<Order> 
        { 
            new Order 
            { 
                Id = Guid.NewGuid(), 
                Product = product, 
                Amount = 5,
                CreatedOn = beforeWindow // Order outside 12-hour window
            }
        };
        users.Add(user);
        
        _mockProductRepository.GetById(productId).Returns(product);
        _mockUserRepository.GetFirst12WithOrders().Returns(users);

        // Act
        var result = await _sut.Get12HourlyLiveLeaderboard(productId);

        // Assert
        result.Should().BeEmpty();
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetFirst12WithOrders();
    }

    #endregion

    #region DeterminePositions Tests

    [TestMethod]
    public void DeterminePositions_OrdersEntriesByAmountDescending()
    {
        // Arrange
        var entries = new List<LeaderboardEntry>
        {
            new LeaderboardEntry { Amount = 5, FullName = "User1", ProductName = "Product" },
            new LeaderboardEntry { Amount = 10, FullName = "User2", ProductName = "Product" },
            new LeaderboardEntry { Amount = 3, FullName = "User3", ProductName = "Product" }
        };

        // Act
        var result = InvokeDeterminePositions(entries);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(3);
        resultList[0].Amount.Should().Be(10);
        resultList[0].Position.Should().Be(1);
        resultList[1].Amount.Should().Be(5);
        resultList[1].Position.Should().Be(2);
        resultList[2].Amount.Should().Be(3);
        resultList[2].Position.Should().Be(3);
    }

    [TestMethod]
    public void DeterminePositions_HandlesTiedScores()
    {
        // Arrange
        var entries = new List<LeaderboardEntry>
        {
            new LeaderboardEntry { Amount = 5, FullName = "User1", ProductName = "Product" },
            new LeaderboardEntry { Amount = 10, FullName = "User2", ProductName = "Product" },
            new LeaderboardEntry { Amount = 5, FullName = "User3", ProductName = "Product" }
        };

        // Act
        var result = InvokeDeterminePositions(entries);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(3);
        resultList[0].Amount.Should().Be(10);
        resultList[0].Position.Should().Be(1);
        resultList[1].Amount.Should().Be(5);
        resultList[1].Position.Should().Be(2);
        resultList[2].Amount.Should().Be(5);
        resultList[2].Position.Should().Be(2); // Same position for tied scores
    }

    [TestMethod]
    public void DeterminePositions_HandlesZeroScores()
    {
        // Arrange
        var entries = new List<LeaderboardEntry>
        {
            new LeaderboardEntry { Amount = 5, FullName = "User1", ProductName = "Product" },
            new LeaderboardEntry { Amount = 0, FullName = "User2", ProductName = "Product" },
            new LeaderboardEntry { Amount = 3, FullName = "User3", ProductName = "Product" }
        };

        // Act
        var result = InvokeDeterminePositions(entries);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(3);
        resultList[0].Amount.Should().Be(5);
        resultList[0].Position.Should().Be(1);
        resultList[1].Amount.Should().Be(3);
        resultList[1].Position.Should().Be(2);
        resultList[2].Amount.Should().Be(0);
        resultList[2].Position.Should().Be(0); // Zero scores get position 0
    }

    [TestMethod]
    public void DeterminePositions_HandlesEmptyList()
    {
        // Arrange
        var entries = new List<LeaderboardEntry>();

        // Act
        var result = InvokeDeterminePositions(entries);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private ApplicationUser CreateApplicationUser(
        string id, 
        string username = "testuser", 
        string name = "Test", 
        string surname = "User",
        string email = "test@example.com")
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = username,
            Name = name,
            Surname = surname,
            Email = email,
            PasswordHash = "hashedpassword",
            Saldo = 100m,
            Orders = new List<Order>()
        };
    }

    private Product CreateProduct(Guid id, string name)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Description = $"Description for {name}",
            Price = 10.0m,
            Stock = 100,
            CreatedOn = DateTime.Now,
            Orders = new List<Order>()
        };
    }

    private List<ApplicationUser> CreateUsersWithOrders(Product product)
    {
        var users = new List<ApplicationUser>();
        
        var user1 = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        user1.Orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 3, CreatedOn = DateTime.Now.AddDays(-1) },
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 2, CreatedOn = DateTime.Now.AddDays(-2) }
        };
        users.Add(user1);
        
        var user2 = CreateApplicationUser("user-2", "user2", "Test2", "User2");
        user2.Orders = new List<Order>
        {
            new Order { Id = Guid.NewGuid(), Product = product, Amount = 3, CreatedOn = DateTime.Now.AddDays(-1) }
        };
        users.Add(user2);
        
        var user3 = CreateApplicationUser("user-3", "user3", "Test3", "User3");
        user3.Orders = new List<Order>
        {
            // This user has orders for a different product
            new Order 
            { 
                Id = Guid.NewGuid(), 
                Product = new Product { Id = Guid.NewGuid(), Name = "Different Product" },
                Amount = 5, 
                CreatedOn = DateTime.Now.AddDays(-1)
            }
        };
        users.Add(user3);
        
        return users;
    }

    // Helper method to invoke private DeterminePositions method using reflection
    private IEnumerable<LeaderboardEntry> InvokeDeterminePositions(IEnumerable<LeaderboardEntry> entries)
    {
        var methodInfo = typeof(ApplicationUserService).GetMethod(
            "DeterminePositions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        return (IEnumerable<LeaderboardEntry>)methodInfo!.Invoke(null, new object[] { entries })!;
    }

    #endregion
}