using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Test.Services;

[TestClass]
public class OrderServiceTests
{
    private IOrderRepository _mockOrderRepository = null!;
    private IPurchaseNotifier _purchaseNotifier = null!;
    private IAchievementService _achievementService = null!;
    private IProductService _productService = null!;
    private IApplicationUserService _applicationUserService = null!;
    private ILogger<OrderService> _mockLogger = null!;
    private OrderService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockOrderRepository = Substitute.For<IOrderRepository>();
        _purchaseNotifier = Substitute.For<IPurchaseNotifier>();
        _achievementService = Substitute.For<IAchievementService>();
        _productService = Substitute.For<IProductService>();
        _applicationUserService = Substitute.For<IApplicationUserService>();
        _mockLogger = Substitute.For<ILogger<OrderService>>();
        _sut = new OrderService(_mockOrderRepository,
            _achievementService,
            _purchaseNotifier,
            _productService,
            _applicationUserService,
            _mockLogger);
    }

    #region GetAllQueryable Tests

    [TestMethod]
    public void GetAllQueryable_ShouldReturnQueryableFromRepository()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateOrder(Guid.NewGuid(), "user1", "Product 1", 2, 10.00m),
            CreateOrder(Guid.NewGuid(), "user2", "Product 2", 1, 5.00m)
        }.AsQueryable();

        _mockOrderRepository.GetAllQueryable(Arg.Any<ApplicationDbContext>()).Returns(orders);

        // Act
        var result = _sut.GetAllQueryable(null!);

        // Assert
        result.Should().BeEquivalentTo(orders);
        _mockOrderRepository.Received(1).GetAllQueryable(Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetAllQueryable_WhenRepositoryReturnsEmptyQueryable_ShouldReturnEmptyQueryable()
    {
        // Arrange
        var emptyQueryable = new List<Order>().AsQueryable();
        _mockOrderRepository.GetAllQueryable(Arg.Any<ApplicationDbContext>()).Returns(emptyQueryable);

        // Act
        var result = _sut.GetAllQueryable(null!);

        // Assert
        result.Should().BeEmpty();
        _mockOrderRepository.Received(1).GetAllQueryable(Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetAllQueryable_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockOrderRepository.GetAllQueryable(Arg.Any<ApplicationDbContext>()).Returns(_ => 
            throw new InvalidOperationException("Database error"));

        // Act
        Action act = () => _sut.GetAllQueryable(null!);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Database error");
        _mockOrderRepository.Received(1).GetAllQueryable(Arg.Any<ApplicationDbContext>());
    }
    
    #endregion

    #region GetPersonalQueryable Tests

    [TestMethod]
    public void GetPersonalQueryable_WithValidUsername_ShouldReturnQueryableFromRepository()
    {
        // Arrange
        var username = "testuser";
        var orders = new List<Order>
        {
            CreateOrder(Guid.NewGuid(), username, "Product 1", 2, 10.00m),
            CreateOrder(Guid.NewGuid(), username, "Product 2", 1, 5.00m)
        }.AsQueryable();

        _mockOrderRepository.GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>()).Returns(orders);

        // Act
        var result = _sut.GetPersonalQueryable(username, null!);

        // Assert
        result.Should().BeEquivalentTo(orders);
        _mockOrderRepository.Received(1).GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetPersonalQueryable_WhenRepositoryReturnsEmptyQueryable_ShouldReturnEmptyQueryable()
    {
        // Arrange
        var username = "testuser";
        var emptyQueryable = new List<Order>().AsQueryable();
        _mockOrderRepository.GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>()).Returns(emptyQueryable);

        // Act
        var result = _sut.GetPersonalQueryable(username, null!);

        // Assert
        result.Should().BeEmpty();
        _mockOrderRepository.Received(1).GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>());
    }

    [TestMethod]
    public void GetPersonalQueryable_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var username = "testuser";
        _mockOrderRepository.GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>()).Returns(_ => 
            throw new InvalidOperationException("Database error"));

        // Act
        Action act = () => _sut.GetPersonalQueryable(username, null!);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Database error");
        _mockOrderRepository.Received(1).GetPersonalQueryable(username, Arg.Any<ApplicationDbContext>());
    }

    #endregion

    #region GetLatestOrders Tests

    [TestMethod]
    public async Task GetLatestOrders_ShouldReturnOrdersFromRepository()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateOrder(Guid.NewGuid(), "user1", "Product 1", 2, 10.00m),
            CreateOrder(Guid.NewGuid(), "user2", "Product 2", 1, 5.00m)
        };

        _mockOrderRepository.GetLatest().Returns(orders);

        // Act
        var result = await _sut.GetLatestOrders();

        // Assert
        result.Should().BeEquivalentTo(orders);
        await _mockOrderRepository.Received(1).GetLatest();
    }

    [TestMethod]
    public async Task GetLatestOrders_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyList = new List<Order>();
        _mockOrderRepository.GetLatest().Returns(emptyList);

        // Act
        var result = await _sut.GetLatestOrders();

        // Assert
        result.Should().BeEmpty();
        await _mockOrderRepository.Received(1).GetLatest();
    }

    [TestMethod]
    public async Task GetLatestOrders_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockOrderRepository.GetLatest().Returns(Task.FromException<IEnumerable<Order>>(
            new InvalidOperationException("Database error")));

        // Act
        Func<Task> act = async () => await _sut.GetLatestOrders();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
        await _mockOrderRepository.Received(1).GetLatest();
    }

    #endregion

    #region GetOrderById Tests

    [TestMethod]
    public async Task GetOrderById_WithValidId_ShouldReturnOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, "user1", "Product 1", 2, 10.00m);
        _mockOrderRepository.GetById(orderId).Returns(order);

        // Act
        var result = await _sut.GetOrderById(orderId);

        // Assert
        result.Should().BeEquivalentTo(order);
        await _mockOrderRepository.Received(1).GetById(orderId);
    }

    [TestMethod]
    public async Task GetOrderById_WithNonExistentId_ShouldPropagateNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.GetById(orderId).Returns(Task.FromException<Order>(
            new NotFoundException("Order not found")));

        // Act
        Func<Task> act = async () => await _sut.GetOrderById(orderId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Order not found");
        await _mockOrderRepository.Received(1).GetById(orderId);
    }

    [TestMethod]
    public async Task GetOrderById_WithEmptyGuid_ShouldPropagateArgumentException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        _mockOrderRepository.GetById(emptyGuid).Returns(Task.FromException<Order>(
            new ArgumentException("Invalid order ID")));

        // Act
        Func<Task> act = async () => await _sut.GetOrderById(emptyGuid);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid order ID");
        await _mockOrderRepository.Received(1).GetById(emptyGuid);
    }

    #endregion

    #region ExportOrdersToCsvAsync Tests

    [TestMethod]
    public async Task ExportOrdersToCsvAsync_WithOrders_ShouldReturnValidCsv()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateOrder(Guid.NewGuid(), "user1", "Product 1", 2, 10.00m, new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreateOrder(Guid.NewGuid(), "user2", "Product 2", 3, 15.00m, new DateTime(2025, 9, 2, 0, 0, 0, DateTimeKind.Utc))
        };

        _mockOrderRepository.GetOrdersFromPastTwoYearsAsync().Returns(orders);

        // Act
        var result = await _sut.ExportOrdersFromPastTwoYearsToCsvAsync();

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().StartWith("OrderId,CustomerUsername,OrderDateTime,ProductName,ProductAmount,TotalPaid");

        // Check that each order is in the CSV
        foreach (var order in orders)
        {
            result.Should().Contain($"{order.Id},\"{order.User.UserName}\",{order.CreatedOn:yyyy-MM-dd HH:mm:ss},\"{order.ProductNaam}\",{order.Amount},{order.Paid.ToString(CultureInfo.InvariantCulture)}");
        }

        await _mockOrderRepository.Received(1).GetOrdersFromPastTwoYearsAsync();
    }

    [TestMethod]
    public async Task ExportOrdersToCsvAsync_WithNoOrders_ShouldReturnHeaderOnly()
    {
        // Arrange
        _mockOrderRepository.GetOrdersFromPastTwoYearsAsync().Returns(new List<Order>());

        // Act
        var result = await _sut.ExportOrdersFromPastTwoYearsToCsvAsync();

        // Assert
        result.Should().Be("OrderId,CustomerUsername,OrderDateTime,ProductName,ProductAmount,TotalPaid" + Environment.NewLine);
        await _mockOrderRepository.Received(1).GetOrdersFromPastTwoYearsAsync();
    }

    [TestMethod]
    public async Task ExportOrdersToCsvAsync_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockOrderRepository.GetOrdersFromPastTwoYearsAsync().Returns(Task.FromException<IList<Order>>(
            new InvalidOperationException("Database error")));

        // Act
        Func<Task> act = async () => await _sut.ExportOrdersFromPastTwoYearsToCsvAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
        await _mockOrderRepository.Received(1).GetOrdersFromPastTwoYearsAsync();
    }

    #endregion

    #region DeleteOrder Tests

    [TestMethod]
    public async Task DeleteOrder_WithValidId_ShouldCallRepository()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        await _sut.DeleteOrder(orderId);

        // Assert
        await _mockOrderRepository.Received(1).Delete(orderId);
    }

    [TestMethod]
    public async Task DeleteOrder_WithNonExistentId_ShouldPropagateNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.Delete(orderId).Returns(Task.FromException(
            new NotFoundException("Order not found")));

        // Act
        Func<Task> act = async () => await _sut.DeleteOrder(orderId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Order not found");
        await _mockOrderRepository.Received(1).Delete(orderId);
    }

    [TestMethod]
    public async Task DeleteOrder_WithEmptyGuid_ShouldPropagateArgumentException()
    {
        // Arrange
        var emptyGuid = Guid.Empty;
        _mockOrderRepository.Delete(emptyGuid).Returns(Task.FromException(
            new ArgumentException("Invalid order ID")));

        // Act
        Func<Task> act = async () => await _sut.DeleteOrder(emptyGuid);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid order ID");
        await _mockOrderRepository.Received(1).Delete(emptyGuid);
    }

    #endregion

    #region Helper Methods

    private static Order CreateOrder(
        Guid id, 
        string username, 
        string productName, 
        int amount, 
        decimal paid,
        DateTime? createdOn = null)
    {
        return new Order
        {
            Id = id,
            User = new ApplicationUser { UserName = username },
            ProductNaam = productName,
            Amount = amount,
            Paid = paid,
            CreatedOn = createdOn ?? DateTime.UtcNow
        };
    }

    private static Product CreateProduct(Guid id, string name, decimal price)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Price = price,
            Stock = 100
        };
    }

    private static ApplicationUser CreateUser(string username)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            Email = $"{username}@example.com",
            Name = $"Test {username}",
            Surname = "User"
        };
    }

    #endregion
}