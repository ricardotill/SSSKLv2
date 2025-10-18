using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;
using SSSKLv2.Components.Pages;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Test.Services;

[TestClass]
public class OrderServiceTests
{
    private IOrderRepository _mockOrderRepository = null!;
    private IPurchaseNotifier _purchaseNotifier = null!;
    private ILogger<OrderService> _mockLogger = null!;
    private OrderService _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockOrderRepository = Substitute.For<IOrderRepository>();
        _purchaseNotifier = Substitute.For<IPurchaseNotifier>();
        _mockLogger = Substitute.For<ILogger<OrderService>>();
        _sut = new OrderService(_mockOrderRepository, _purchaseNotifier, _mockLogger);
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

    #region CreateOrder Tests

    [TestMethod]
    public async Task CreateOrder_WithValidInputs_ShouldCreateOrdersForAllSelectedProductsAndUsers()
    {
        // Arrange
        var orderDto = new POS.BestellingDto
        {
            Products = new List<POS.Select<Product>>
            {
                new() { Selected = true, Value = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m) },
                new() { Selected = true, Value = CreateProduct(Guid.NewGuid(), "Product 2", 15.00m) },
                new() { Selected = false, Value = CreateProduct(Guid.NewGuid(), "Product 3", 20.00m) }
            },
            Users = new List<POS.Select<ApplicationUser>>
            {
                new() { Selected = true, Value = CreateUser("user1") },
                new() { Selected = true, Value = CreateUser("user2") },
                new() { Selected = false, Value = CreateUser("user3") }
            },
            Amount = 2,
            Split = false // Each user pays full price
        };

        // Act
        await _sut.CreateOrder(orderDto);

        // Assert
        // 2 selected products * 2 selected users = 4 orders
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<List<Order>>(
            orders => orders.Count == 4 && 
                     orders.Count(o => o.ProductNaam == "Product 1") == 2 &&
                     orders.Count(o => o.ProductNaam == "Product 2") == 2 &&
                     orders.All(o => o.Amount == 2) && // Each order has full amount
                     orders.Count(o => o.Paid == 20.00m) == 2 && // Product 1: 10.00 * 2
                     orders.Count(o => o.Paid == 30.00m) == 2)); // Product 2: 15.00 * 2
    }

    [TestMethod]
    public async Task CreateOrder_WithSplitEnabled_ShouldDivideAmountAndPriceAmongUsers()
    {
        // Arrange
        var orderDto = new POS.BestellingDto
        {
            Products = new List<POS.Select<Product>>
            {
                new() { Selected = true, Value = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m) }
            },
            Users = new List<POS.Select<ApplicationUser>>
            {
                new() { Selected = true, Value = CreateUser("user1") },
                new() { Selected = true, Value = CreateUser("user2") }
            },
            Amount = 3, // 3 items
            Split = true // Split between users
        };

        // Act
        await _sut.CreateOrder(orderDto);

        // Assert
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<List<Order>>(
            orders => orders.Count == 2 &&
                     orders.All(o => o.ProductNaam == "Product 1") &&
                     orders.All(o => o.Amount == 1) && // 3 items split between 2 users = 1 per user (with rounding down)
                     orders.All(o => o.Paid == 15.00m))); // 10.00 per item * 3 items / 2 users = 15.00 per user
    }

    [TestMethod]
    public async Task CreateOrder_WithNoSelectedProducts_ShouldNotCreateAnyOrders()
    {
        // Arrange
        var orderDto = new POS.BestellingDto
        {
            Products = new List<POS.Select<Product>>
            {
                new() { Selected = false, Value = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m) }
            },
            Users = new List<POS.Select<ApplicationUser>>
            {
                new() { Selected = true, Value = CreateUser("user1") }
            },
            Amount = 1,
            Split = false
        };

        // Act
        await _sut.CreateOrder(orderDto);

        // Assert
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<List<Order>>(orders => orders.Count == 0));
    }

    [TestMethod]
    public async Task CreateOrder_WithNoSelectedUsers_ShouldNotCreateAnyOrders()
    {
        // Arrange
        var orderDto = new POS.BestellingDto
        {
            Products = new List<POS.Select<Product>>
            {
                new() { Selected = true, Value = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m) }
            },
            Users = new List<POS.Select<ApplicationUser>>
            {
                new() { Selected = false, Value = CreateUser("user1") }
            },
            Amount = 1,
            Split = false
        };

        // Act
        await _sut.CreateOrder(orderDto);

        // Assert
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<List<Order>>(orders => orders.Count == 0));
    }

    [TestMethod]
    public async Task CreateOrder_WithZeroAmount_ShouldCreateOrdersWithZeroAmountAndPaid()
    {
        // Arrange
        var orderDto = new POS.BestellingDto
        {
            Products = new List<POS.Select<Product>>
            {
                new() { Selected = true, Value = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m) }
            },
            Users = new List<POS.Select<ApplicationUser>>
            {
                new() { Selected = true, Value = CreateUser("user1") }
            },
            Amount = 0, // Zero amount
            Split = false
        };

        // Act
        await _sut.CreateOrder(orderDto);

        // Assert
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<List<Order>>(
            orders => orders.Count == 1 &&
                     orders[0].Amount == 0 &&
                     orders[0].Paid == 0m));
    }

    [TestMethod]
    public async Task CreateOrder_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var orderDto = new POS.BestellingDto
        {
            Products = new List<POS.Select<Product>>
            {
                new() { Selected = true, Value = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m) }
            },
            Users = new List<POS.Select<ApplicationUser>>
            {
                new() { Selected = true, Value = CreateUser("user1") }
            },
            Amount = 1,
            Split = false
        };

        _mockOrderRepository.CreateRange(Arg.Any<List<Order>>()).Returns(Task.FromException(
            new InvalidOperationException("Failed to create orders")));

        // Act
        Func<Task> act = async () => await _sut.CreateOrder(orderDto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to create orders");
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

    #region GenerateUserOrders Tests (testing private method via CreateOrder)

    [TestMethod]
    public async Task GenerateUserOrders_WithSplitFalseAndMultipleUsers_ShouldGiveEachUserFullAmount()
    {
        // Arrange
        var orderDto = new POS.BestellingDto
        {
            Products = new List<POS.Select<Product>>
            {
                new() { Selected = true, Value = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m) }
            },
            Users = new List<POS.Select<ApplicationUser>>
            {
                new() { Selected = true, Value = CreateUser("user1") },
                new() { Selected = true, Value = CreateUser("user2") }
            },
            Amount = 5,
            Split = false // Each user gets full amount
        };

        // Act
        await _sut.CreateOrder(orderDto);

        // Assert
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<List<Order>>(
            orders => orders.Count == 2 &&
                     orders.All(o => o.Amount == 5) && // Each user gets full amount
                     orders.All(o => o.Paid == 50.00m))); // Each user pays full price (10 * 5)
    }

    [TestMethod]
    public async Task GenerateUserOrders_WithSplitTrueAndOddAmount_ShouldHandleRoundingCorrectly()
    {
        // Arrange - 3 items split between 2 users
        var orderDto = new POS.BestellingDto
        {
            Products = new List<POS.Select<Product>>
            {
                new() { Selected = true, Value = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m) }
            },
            Users = new List<POS.Select<ApplicationUser>>
            {
                new() { Selected = true, Value = CreateUser("user1") },
                new() { Selected = true, Value = CreateUser("user2") }
            },
            Amount = 3,
            Split = true // Split between users
        };

        // Act
        await _sut.CreateOrder(orderDto);

        // Assert
        // 3 items ÷ 2 users = 1.5 items per user (rounded down to 1)
        // Total paid should still be correct: 10.00 * 3 / 2 = 15.00 per user
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<List<Order>>(
            orders => orders.Count == 2 &&
                     orders.All(o => o.Amount == 1) && // Integer division rounds down
                     orders.All(o => o.Paid == 15.00m))); // But payment is exact division
    }

    [TestMethod]
    public async Task GenerateUserOrders_WithSplitTrueAndPriceRounding_ShouldRoundToPositiveInfinity()
    {
        // Arrange - Price that doesn't divide evenly
        var orderDto = new POS.BestellingDto
        {
            Products = new List<POS.Select<Product>>
            {
                new() { Selected = true, Value = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m) }
            },
            Users = new List<POS.Select<ApplicationUser>>
            {
                new() { Selected = true, Value = CreateUser("user1") },
                new() { Selected = true, Value = CreateUser("user2") },
                new() { Selected = true, Value = CreateUser("user3") }
            },
            Amount = 1,
            Split = true // Split between users
        };

        // Act
        await _sut.CreateOrder(orderDto);

        // Assert
        // 10.00 ÷ 3 users = 3.33... per user (rounds to 3.34)
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<List<Order>>(
            orders => orders.Count == 3 &&
                     orders.All(o => o.Paid == 3.34m))); // Rounded to positive infinity
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