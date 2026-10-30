using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.Orders;
using SSSKL.Application.Features.Orders.Queries.GetLatestOrders;
using SSSKL.Application.Features.Orders.Queries.GetOrderById;
using SSSKL.Application.Features.Orders.Queries.GetOrders;
using SSSKL.Application.Features.Orders.Queries.GetPersonalOrders;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Test.Features.Orders;

[TestClass]
public class OrderQueryHandlerTests
{
    private IOrderRepository _mockOrderRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockOrderRepository = Substitute.For<IOrderRepository>();
    }

    #region GetLatestOrders Tests

    [TestMethod]
    public async Task GetLatestOrders_ShouldReturnOrdersFromRepository()
    {
        // Arrange
        var orders = new List<Order>
        {
            OrderTestData.CreateOrder(Guid.NewGuid(), "user1", "Product 1", 2, 10.00m),
            OrderTestData.CreateOrder(Guid.NewGuid(), "user2", "Product 2", 1, 5.00m)
        };
        _mockOrderRepository.GetLatest(6).Returns(orders);
        var sut = new GetLatestOrdersHandler(_mockOrderRepository);

        // Act
        var result = await sut.Handle(new GetLatestOrdersQuery(6), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(orders.Select(o => o.ToDto()));
        await _mockOrderRepository.Received(1).GetLatest(6);
    }

    [TestMethod]
    public async Task GetLatestOrders_WhenRepositoryReturnsEmptyList_ShouldReturnEmptyList()
    {
        // Arrange
        _mockOrderRepository.GetLatest(Arg.Any<int>()).Returns(new List<Order>());
        var sut = new GetLatestOrdersHandler(_mockOrderRepository);

        // Act
        var result = await sut.Handle(new GetLatestOrdersQuery(10), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetLatestOrders_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        _mockOrderRepository.GetLatest(Arg.Any<int>()).Returns(Task.FromException<IEnumerable<Order>>(
            new InvalidOperationException("Database error")));
        var sut = new GetLatestOrdersHandler(_mockOrderRepository);

        // Act
        Func<Task> act = () => sut.Handle(new GetLatestOrdersQuery(10), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database error");
    }

    #endregion

    #region GetOrderById Tests

    [TestMethod]
    public async Task GetOrderById_WithValidId_ShouldReturnOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = OrderTestData.CreateOrder(orderId, "user1", "Product 1", 2, 10.00m);
        _mockOrderRepository.GetById(orderId).Returns(order);
        var sut = new GetOrderByIdHandler(_mockOrderRepository);

        // Act
        var result = await sut.Handle(new GetOrderByIdQuery(orderId, "user1-id", false), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(order.ToDto());
    }

    [TestMethod]
    public async Task GetOrderById_AsAdminForOtherUsersOrder_ShouldReturnOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = OrderTestData.CreateOrder(orderId, "user1", "Product 1", 2, 10.00m);
        _mockOrderRepository.GetById(orderId).Returns(order);
        var sut = new GetOrderByIdHandler(_mockOrderRepository);

        // Act
        var result = await sut.Handle(new GetOrderByIdQuery(orderId, "admin-id", true), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(order.ToDto());
    }

    [TestMethod]
    public async Task GetOrderById_AsNonOwner_ShouldThrowUnauthorizedAccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = OrderTestData.CreateOrder(orderId, "user1", "Product 1", 2, 10.00m);
        _mockOrderRepository.GetById(orderId).Returns(order);
        var sut = new GetOrderByIdHandler(_mockOrderRepository);

        // Act
        Func<Task> act = () => sut.Handle(new GetOrderByIdQuery(orderId, "user2-id", false), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task GetOrderById_WithNonExistentId_ShouldPropagateNotFoundException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _mockOrderRepository.GetById(orderId).Throws(new NotFoundException("Order not found"));
        var sut = new GetOrderByIdHandler(_mockOrderRepository);

        // Act
        Func<Task> act = () => sut.Handle(new GetOrderByIdQuery(orderId, "admin-id", true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Order not found");
    }

    [TestMethod]
    public async Task GetOrderById_WithEmptyGuid_ShouldPropagateArgumentException()
    {
        // Arrange
        _mockOrderRepository.GetById(Guid.Empty).Throws(new ArgumentException("Invalid order ID"));
        var sut = new GetOrderByIdHandler(_mockOrderRepository);

        // Act
        Func<Task> act = () => sut.Handle(new GetOrderByIdQuery(Guid.Empty, "admin-id", true), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid order ID");
    }

    #endregion

    #region Paged Get Tests

    [TestMethod]
    public async Task GetOrders_ShouldCallRepositoryWithCorrectPagingAndReturnCount()
    {
        // Arrange
        _mockOrderRepository.GetAll(10, 5).Returns(new List<Order> { OrderTestData.CreateOrder(Guid.NewGuid(), "u", "P", 1, 1m) });
        _mockOrderRepository.GetCount().Returns(11);
        var sut = new GetOrdersHandler(_mockOrderRepository);

        // Act
        var result = await sut.Handle(new GetOrdersQuery(10, 5), CancellationToken.None);

        // Assert
        await _mockOrderRepository.Received(1).GetAll(10, 5);
        result.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(11);
    }

    [TestMethod]
    public async Task GetPersonalOrders_ShouldCallRepositoryWithCorrectPagingAndReturnCount()
    {
        // Arrange
        _mockOrderRepository.GetPersonal("testuser", 0, 10).Returns(new List<Order>());
        _mockOrderRepository.GetPersonalCount("testuser").Returns(0);
        var sut = new GetPersonalOrdersHandler(_mockOrderRepository);

        // Act
        var result = await sut.Handle(new GetPersonalOrdersQuery("testuser", 0, 10), CancellationToken.None);

        // Assert
        await _mockOrderRepository.Received(1).GetPersonal("testuser", 0, 10);
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion
}
