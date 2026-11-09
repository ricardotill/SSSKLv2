using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Orders.Commands.CreateOrder;
using SSSKL.Application.Features.Orders.Commands.DeleteOrder;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;
using SSSKL.Domain.Exceptions;
using static SSSKL.Application.Test.Features.Orders.OrderTestData;

namespace SSSKL.Application.Test.Features.Orders;

[TestClass]
public class OrderCommandHandlerTests
{
    private IOrderRepository _mockOrderRepository = null!;
    private IProductRepository _productRepository = null!;
    private IApplicationUserRepository _userRepository = null!;
    private IPurchaseNotifier _purchaseNotifier = null!;
    private IUserNotifier _userNotifier = null!;
    private IDomainEventDispatcher _mockEventDispatcher = null!;
    private IUserStatRepository _mockUserStatRepository = null!;
    private CreateOrderHandler _createSut = null!;
    private DeleteOrderHandler _deleteSut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockOrderRepository = Substitute.For<IOrderRepository>();
        _productRepository = Substitute.For<IProductRepository>();
        _userRepository = Substitute.For<IApplicationUserRepository>();
        _purchaseNotifier = Substitute.For<IPurchaseNotifier>();
        _userNotifier = Substitute.For<IUserNotifier>();
        _mockEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        _mockUserStatRepository = Substitute.For<IUserStatRepository>();
        _createSut = new CreateOrderHandler(
            _mockOrderRepository,
            _productRepository,
            _userRepository,
            _purchaseNotifier,
            _userNotifier,
            _mockEventDispatcher,
            NullLogger<CreateOrderHandler>.Instance);
        _deleteSut = new DeleteOrderHandler(_mockOrderRepository, _mockUserStatRepository);
    }

    private Task Create(OrderSubmitDto dto, string? actingUserId) =>
        _createSut.Handle(new CreateOrderCommand(dto, actingUserId), CancellationToken.None);

    #region DeleteOrder Tests

    [TestMethod]
    public async Task DeleteOrder_AsAdmin_ShouldDeleteAndRecalculateStats()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, "user1", "Product 1", 1, 10m);
        _mockOrderRepository.GetById(orderId).Returns(order);

        // Act
        await _deleteSut.Handle(new DeleteOrderCommand(orderId, "admin-id", IsAdmin: true), CancellationToken.None);

        // Assert
        await _mockOrderRepository.Received(1).Delete(orderId);
        await _mockUserStatRepository.Received(1).RecalculateByUserId(order.User.Id);
    }

    [TestMethod]
    public async Task DeleteOrder_AsOwner_ShouldDelete()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, "user1", "Product 1", 1, 10m);
        _mockOrderRepository.GetById(orderId).Returns(order);

        // Act
        await _deleteSut.Handle(new DeleteOrderCommand(orderId, order.User.Id.ToUpperInvariant(), IsAdmin: false), CancellationToken.None);

        // Assert
        await _mockOrderRepository.Received(1).Delete(orderId);
    }

    [TestMethod]
    public async Task DeleteOrder_AsOtherUser_ShouldThrowUnauthorizedAndNotDelete()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.GetById(orderId).Returns(CreateOrder(orderId, "user1", "Product 1", 1, 10m));

        // Act
        Func<Task> act = () => _deleteSut.Handle(new DeleteOrderCommand(orderId, "someone-else", IsAdmin: false), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _mockOrderRepository.DidNotReceive().Delete(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task DeleteOrder_WithNonExistentId_ShouldPropagateNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.GetById(orderId).Throws(new NotFoundException("Order not found"));

        // Act
        Func<Task> act = () => _deleteSut.Handle(new DeleteOrderCommand(orderId, null, IsAdmin: true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Order not found");
        await _mockOrderRepository.DidNotReceive().Delete(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task DeleteOrder_WhenRepositoryDeleteThrows_ShouldPropagateException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.GetById(orderId).Returns(CreateOrder(orderId, "user1", "Product 1", 1, 10m));
        _mockOrderRepository.Delete(orderId).Returns(Task.FromException(new ArgumentException("Invalid order ID")));

        // Act
        Func<Task> act = () => _deleteSut.Handle(new DeleteOrderCommand(orderId, null, IsAdmin: true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid order ID");
    }

    #endregion

    #region CreateOrder Tests

    [TestMethod]
    public async Task CreateOrder_WithNullDto_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = () => Create(null!, null);

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

        _userRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { user });
        _productRepository.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Product> { product });

        // Act
        await Create(dto, userId.ToString());

        // Assert
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<IEnumerable<Order>>(
            orders => orders.Count() == 1 &&
                      orders.First().ProductNaam == product.Name &&
                      orders.First().Paid == product.Price));
        await _purchaseNotifier.Received(1).NotifyUserPurchaseAsync(Arg.Any<UserPurchaseEvent>());
        await _mockEventDispatcher.Received(1).DispatchAsync(Arg.Any<OrderPlacedEvent>());
    }

    [TestMethod]
    public async Task CreateOrder_WhenSideEffectsFailAfterSave_DoesNotThrow()
    {
        // Arrange
        var user = CreateUser("testuser");
        var otherUser = CreateUser("other");
        var product = CreateProduct(Guid.NewGuid(), "Product 1", 10.00m);
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            Products = new List<Guid> { product.Id },
            Amount = 1,
            Split = false
        };

        _userRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { user, otherUser });
        _productRepository.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Product> { product });
        _purchaseNotifier.NotifyUserPurchaseAsync(Arg.Any<UserPurchaseEvent>())
            .Returns(Task.FromException(new InvalidOperationException("SignalR down")));
        _userRepository.GetById(Arg.Any<string>())
            .Returns(Task.FromException<ApplicationUser>(new InvalidOperationException("DB hiccup")));

        // Act
        Func<Task> act = () => Create(dto, "acting-user");

        // Assert
        await act.Should().NotThrowAsync();
        await _mockOrderRepository.Received(1).CreateRange(Arg.Any<IEnumerable<Order>>());
        await _mockEventDispatcher.Received(2).DispatchAsync(Arg.Any<OrderPlacedEvent>());
    }

    [TestMethod]
    public async Task CreateOrder_MultipleUsersDutchSplit_ShouldCalculateCorrectPrices()
    {
        // Arrange
        var product = CreateProduct(Guid.NewGuid(), "Expensive Product", 9.99m); // 9.99 / 2 = 5.00 rounded up
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            Products = new List<Guid> { product.Id },
            Amount = 1,
            Split = true
        };

        _userRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { CreateUser("user1"), CreateUser("user2") });
        _productRepository.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Product> { product });

        // Act
        await Create(dto, null);

        // Assert
        // Decimal.Round(9.99 / 2, 2, ToPositiveInfinity) = 5.00
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<IEnumerable<Order>>(
            orders => orders.Count() == 2 && orders.All(o => o.Paid == 5.00m)));
    }

    [TestMethod]
    public async Task CreateOrder_MultipleUsersNoSplit_ShouldChargeFullPriceToEach()
    {
        // Arrange
        var product = CreateProduct(Guid.NewGuid(), "Normal Product", 2.50m);
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            Products = new List<Guid> { product.Id },
            Amount = 1,
            Split = false
        };

        _userRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { CreateUser("user1"), CreateUser("user2") });
        _productRepository.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Product> { product });

        // Act
        await Create(dto, null);

        // Assert
        await _mockOrderRepository.Received(1).CreateRange(Arg.Is<IEnumerable<Order>>(
            orders => orders.Count() == 2 && orders.All(o => o.Paid == 2.50m)));
        await _purchaseNotifier.Received(2).NotifyUserPurchaseAsync(Arg.Any<UserPurchaseEvent>());
        await _mockEventDispatcher.Received(2).DispatchAsync(Arg.Any<OrderPlacedEvent>());
    }

    [TestMethod]
    public async Task CreateOrder_WhenProductNotFound_ShouldPropagateNotFoundException()
    {
        // Arrange
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { Guid.NewGuid() },
            Products = new List<Guid> { Guid.NewGuid() },
            Amount = 1
        };
        _productRepository.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Product>());

        // Act
        Func<Task> act = () => Create(dto, null);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Product not found");
        await _mockOrderRepository.DidNotReceiveWithAnyArgs().CreateRange(null!);
    }

    [TestMethod]
    public async Task CreateOrder_WhenUserNotFound_ShouldPropagateNotFoundException()
    {
        // Arrange
        var product = CreateProduct(Guid.NewGuid(), "Product", 1.0m);
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { Guid.NewGuid() },
            Products = new List<Guid> { product.Id },
            Amount = 1
        };
        _productRepository.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Product> { product });
        _userRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser>());

        // Act
        Func<Task> act = () => Create(dto, null);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("ApplicationUser not found");
        await _mockOrderRepository.DidNotReceiveWithAnyArgs().CreateRange(null!);
    }

    [TestMethod]
    public async Task CreateOrder_OnBehalfOf_ShouldSendNotification()
    {
        // Arrange
        var actingUser = CreateUser("actor");
        actingUser.Name = "Ricardo";
        actingUser.Surname = "Till";
        var targetUser = CreateUser("target");
        var product = CreateProduct(Guid.NewGuid(), "Beer", 1.50m);
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { Guid.Parse(actingUser.Id), Guid.Parse(targetUser.Id) },
            Products = new List<Guid> { product.Id },
            Amount = 1,
            Split = false
        };

        _userRepository.GetById(actingUser.Id).Returns(actingUser);
        _userRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { actingUser, targetUser });
        _productRepository.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Product> { product });

        // Act
        await Create(dto, actingUser.Id);

        // Assert
        // Target user should get a notification
        await _userNotifier.Received(1).NotifyAsync(
            targetUser.Id,
            "Nieuwe bestelling!",
            Arg.Is<string>(s => s.Contains("Ricardo T") && s.Contains("Beer")),
            "/orders/personal",
            sendPush: true);

        // Acting user should NOT get a notification
        await _userNotifier.DidNotReceive().NotifyAsync(
            actingUser.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [TestMethod]
    public async Task CreateOrder_OnBehalfOf_MissingActingUser_ShouldUseFallbackName()
    {
        // Arrange
        var actingUserId = Guid.NewGuid().ToString();
        var targetUser = CreateUser("target");
        var product = CreateProduct(Guid.NewGuid(), "Cola", 1.00m);
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { Guid.Parse(targetUser.Id) },
            Products = new List<Guid> { product.Id },
            Amount = 1
        };

        _userRepository.GetById(actingUserId).Returns((ApplicationUser)null!);
        _userRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { targetUser });
        _productRepository.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Product> { product });

        // Act
        await Create(dto, actingUserId);

        // Assert
        await _userNotifier.Received(1).NotifyAsync(
            targetUser.Id,
            "Nieuwe bestelling!",
            Arg.Is<string>(s => s.Contains("Iemand") && s.Contains("Cola")),
            "/orders/personal",
            sendPush: true);
    }

    [TestMethod]
    public async Task CreateOrder_OnBehalfOf_MultipleTargetUsers_ShouldNotifyAllExceptActor()
    {
        // Arrange
        var actingUser = CreateUser("actor");
        var target1 = CreateUser("target1");
        var target2 = CreateUser("target2");
        var product = CreateProduct(Guid.NewGuid(), "Snack", 2.00m);
        var dto = new OrderSubmitDto
        {
            Users = new List<Guid> { Guid.Parse(actingUser.Id), Guid.Parse(target1.Id), Guid.Parse(target2.Id) },
            Products = new List<Guid> { product.Id },
            Amount = 1
        };

        _userRepository.GetById(actingUser.Id).Returns(actingUser);
        _userRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { actingUser, target1, target2 });
        _productRepository.GetByIds(Arg.Any<IEnumerable<Guid>>()).Returns(new List<Product> { product });

        // Act
        await Create(dto, actingUser.Id);

        // Assert
        await _userNotifier.Received(1).NotifyAsync(target1.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), true);
        await _userNotifier.Received(1).NotifyAsync(target2.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), true);
        await _userNotifier.DidNotReceive().NotifyAsync(actingUser.Id, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), true);
    }

    #endregion
}
