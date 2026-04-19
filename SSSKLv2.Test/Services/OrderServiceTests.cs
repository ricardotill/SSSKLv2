using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data.Constants;

namespace SSSKLv2.Test.Services;

[TestClass]
public class OrderServiceTests
{
    private IOrderRepository _mockOrderRepository = null!;
    private IPurchaseNotifier _purchaseNotifier = null!;
    private IAchievementService _achievementService = null!;
    private IProductService _productService = null!;
    private IApplicationUserService _applicationUserService = null!;
    private INotificationService _notificationService = null!;
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
        _notificationService = Substitute.For<INotificationService>();
        _mockLogger = Substitute.For<ILogger<OrderService>>();
        _sut = new OrderService(_mockOrderRepository,
            _achievementService,
            _purchaseNotifier,
            _productService,
            _applicationUserService,
            _notificationService,
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

    #region CreateOrder Tests

    [TestMethod]
    public async Task CreateOrder_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _sut.CreateOrder(null!, null);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task CreateOrder_SingleUserSingleProduct_ShouldCreateSuccessfulOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var user = CreateUser("testuser");
        var product = CreateProduct(productId, "Product 1", 10.00m);
        
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { userId },
            Products = new List<Guid> { productId },
            Amount = 1,
            Split = false
        };

        _applicationUserService.GetUserById(userId.ToString()).Returns(user);
        _productService.GetProductById(productId).Returns(product);

        // Act
        await _sut.CreateOrder(dto, userId.ToString());

        // Assert
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<IEnumerable<Order>>(
            orders => orders.Count() == 1 && 
                      orders.First().ProductNaam == product.Name &&
                      orders.First().Paid == product.Price));
        await _purchaseNotifier.Received(1).NotifyUserPurchaseAsync(Arg.Any<UserPurchaseEvent>());
        await _achievementService.Received(1).CheckOrdersForAchievements(Arg.Any<IEnumerable<Order>>());
    }

    [TestMethod]
    public async Task CreateOrder_MultipleUsersDutchSplit_ShouldCalculateCorrectPrices()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        var product = CreateProduct(productId, "Expensive Product", 9.99m); // 9.99 / 2 = 5.00 rounded up
        
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { user1Id, user2Id },
            Products = new List<Guid> { productId },
            Amount = 1,
            Split = true
        };

        _applicationUserService.GetUserById(user1Id.ToString()).Returns(user1);
        _applicationUserService.GetUserById(user2Id.ToString()).Returns(user2);
        _productService.GetProductById(productId).Returns(product);

        // Act
        await _sut.CreateOrder(dto, user1Id.ToString());

        // Assert
        // Decimal.Round(9.99 / 2, 2, ToPositiveInfinity) = 5.00
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<IEnumerable<Order>>(
            orders => orders.Count() == 2 && 
                      orders.All(o => o.Paid == 5.00m)));
    }

    [TestMethod]
    public async Task CreateOrder_MultipleUsersNoSplit_ShouldChargeFullPriceToEach()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var user1 = CreateUser("user1");
        var user2 = CreateUser("user2");
        var product = CreateProduct(productId, "Normal Product", 2.50m);
        
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { user1Id, user2Id },
            Products = new List<Guid> { productId },
            Amount = 1,
            Split = false
        };

        _applicationUserService.GetUserById(user1Id.ToString()).Returns(user1);
        _applicationUserService.GetUserById(user2Id.ToString()).Returns(user2);
        _productService.GetProductById(productId).Returns(product);

        // Act
        await _sut.CreateOrder(dto, user1Id.ToString());

        // Assert
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<IEnumerable<Order>>(
            orders => orders.Count() == 2 && 
                      orders.All(o => o.Paid == 2.50m)));
        await _purchaseNotifier.Received(2).NotifyUserPurchaseAsync(Arg.Any<UserPurchaseEvent>());
        await _achievementService.Received(1).CheckOrdersForAchievements(Arg.Any<IEnumerable<Order>>());
        await _achievementService.Received(1).CheckUserForAchievements("user1");
        await _achievementService.Received(1).CheckUserForAchievements("user2");
    }

    [TestMethod]
    public async Task CreateOrder_WhenProductNotFound_ShouldPropagateNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var user = CreateUser("user");
        
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { userId },
            Products = new List<Guid> { productId },
            Amount = 1
        };

        _applicationUserService.GetUserById(userId.ToString()).Returns(user);
        _productService.GetProductById(productId).Throws(new NotFoundException("Product not found"));

        // Act
        Func<Task> act = async () => await _sut.CreateOrder(dto, userId.ToString());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Product not found");
        await _mockOrderRepository.DidNotReceiveWithAnyArgs().CreateRange(null!);
    }

    [TestMethod]
    public async Task CreateOrder_WhenUserNotFound_ShouldPropagateNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Product", 1.0m);
        
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { userId },
            Products = new List<Guid> { productId },
            Amount = 1
        };

        _productService.GetProductById(productId).Returns(product);
        _applicationUserService.GetUserById(userId.ToString()).Throws(new NotFoundException("User not found"));

        // Act
        Func<Task> act = async () => await _sut.CreateOrder(dto, userId.ToString());

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("User not found");
        await _mockOrderRepository.DidNotReceiveWithAnyArgs().CreateRange(null!);
    }

    [TestMethod]
    public async Task CreateOrder_OnBehalfOf_ShouldSendNotification()
    {
        // Arrange
        var actingUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        
        var actingUser = CreateUser("actor");
        actingUser.Id = actingUserId.ToString();
        actingUser.Name = "Ricardo";
        actingUser.Surname = "Till";
        
        var targetUser = CreateUser("target");
        targetUser.Id = targetUserId.ToString();
        
        var product = CreateProduct(productId, "Beer", 1.50m);
        
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { actingUserId, targetUserId },
            Products = new List<Guid> { productId },
            Amount = 1,
            Split = false
        };

        _applicationUserService.GetUserById(actingUserId.ToString()).Returns(actingUser);
        _applicationUserService.GetUserById(targetUserId.ToString()).Returns(targetUser);
        _productService.GetProductById(productId).Returns(product);

        // Act
        await _sut.CreateOrder(dto, actingUserId.ToString());

        // Assert
        // Target user should get a notification
        await _notificationService.Received(1).CreateNotificationAsync(
            targetUserId.ToString(),
            "Nieuwe bestelling!",
            Arg.Is<string>(s => s.Contains("Ricardo T") && s.Contains("Beer")),
            "/orders/personal",
            sendPush: true,
            topic: PushTopics.Order);
            
        // Acting user should NOT get a notification
        await _notificationService.DidNotReceive().CreateNotificationAsync(
            actingUserId.ToString(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>());
    }

    [TestMethod]
    public async Task CreateOrder_OnBehalfOf_MissingActingUser_ShouldUseFallbackName()
    {
        // Arrange
        var actingUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        
        var targetUser = CreateUser("target");
        targetUser.Id = targetUserId.ToString();
        var product = CreateProduct(Guid.NewGuid(), "Cola", 1.00m);
        
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { targetUserId },
            Products = new List<Guid> { product.Id },
            Amount = 1
        };

        _applicationUserService.GetUserById(actingUserId.ToString()).Returns((ApplicationUser)null!);
        _applicationUserService.GetUserById(targetUserId.ToString()).Returns(targetUser);
        _productService.GetProductById(product.Id).Returns(product);

        // Act
        await _sut.CreateOrder(dto, actingUserId.ToString());

        // Assert
        await _notificationService.Received(1).CreateNotificationAsync(
            targetUserId.ToString(),
            "Nieuwe bestelling!",
            Arg.Is<string>(s => s.Contains("Iemand") && s.Contains("Cola")),
            "/orders/personal",
            sendPush: true,
            topic: PushTopics.Order);
    }

    [TestMethod]
    public async Task CreateOrder_OnBehalfOf_MultipleTargetUsers_ShouldNotifyAllExceptActor()
    {
        // Arrange
        var actingUserId = Guid.NewGuid();
        var target1Id = Guid.NewGuid();
        var target2Id = Guid.NewGuid();
        
        var actingUser = CreateUser("actor");
        actingUser.Id = actingUserId.ToString();
        var target1 = CreateUser("target1");
        target1.Id = target1Id.ToString();
        var target2 = CreateUser("target2");
        target2.Id = target2Id.ToString();
        
        var product = CreateProduct(Guid.NewGuid(), "Snack", 2.00m);
        
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { actingUserId, target1Id, target2Id },
            Products = new List<Guid> { product.Id },
            Amount = 1
        };

        _applicationUserService.GetUserById(actingUserId.ToString()).Returns(actingUser);
        _applicationUserService.GetUserById(target1Id.ToString()).Returns(target1);
        _applicationUserService.GetUserById(target2Id.ToString()).Returns(target2);
        _productService.GetProductById(product.Id).Returns(product);

        // Act
        await _sut.CreateOrder(dto, actingUserId.ToString());

        // Assert
        await _notificationService.Received(1).CreateNotificationAsync(target1Id.ToString(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), true, PushTopics.Order);
        await _notificationService.Received(1).CreateNotificationAsync(target2Id.ToString(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), true, PushTopics.Order);
        await _notificationService.DidNotReceive().CreateNotificationAsync(actingUserId.ToString(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), true, Arg.Any<string>());
    }

    #endregion

    #region Paged Get Tests

    [TestMethod]
    public async Task GetAll_ShouldCallRepositoryWithCorrectPaging()
    {
        // Arrange
        var skip = 10;
        var take = 5;
        _mockOrderRepository.GetAll(skip, take).Returns(new List<Order>());

        // Act
        await _sut.GetAll(skip, take);

        // Assert
        await _mockOrderRepository.Received(1).GetAll(skip, take);
    }

    [TestMethod]
    public async Task GetPersonal_ShouldCallRepositoryWithCorrectPaging()
    {
        // Arrange
        var username = "testuser";
        var skip = 0;
        var take = 10;
        _mockOrderRepository.GetPersonal(username, skip, take).Returns(new List<Order>());

        // Act
        await _sut.GetPersonal(username, skip, take);

        // Assert
        await _mockOrderRepository.Received(1).GetPersonal(username, skip, take);
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