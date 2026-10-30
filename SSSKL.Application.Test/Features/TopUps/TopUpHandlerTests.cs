using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Application.Features.TopUps.Commands.CreateTopUp;
using SSSKL.Application.Features.TopUps.Commands.DeleteTopUp;
using SSSKL.Application.Features.TopUps.Queries.GetPersonalTopUps;
using SSSKL.Application.Features.TopUps.Queries.GetTopUpById;
using SSSKL.Application.Features.TopUps.Queries.GetTopUps;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Test.Features.TopUps;

[TestClass]
public class TopUpHandlerTests
{
    private ITopUpRepository _mockRepository = null!;
    private IApplicationUserRepository _mockUserRepository = null!;
    private IUserStatRepository _mockUserStatRepository = null!;
    private IDomainEventDispatcher _mockEventDispatcher = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = Substitute.For<ITopUpRepository>();
        _mockUserRepository = Substitute.For<IApplicationUserRepository>();
        _mockUserStatRepository = Substitute.For<IUserStatRepository>();
        _mockEventDispatcher = Substitute.For<IDomainEventDispatcher>();
    }

    private CreateTopUpHandler CreateHandler() => new(_mockRepository, _mockUserRepository, _mockEventDispatcher);

    #region Query Tests

    [TestMethod]
    public async Task GetTopUpById_WithValidId_ShouldReturnDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var topUp = CreateTopUp(id, "testUser", 100m);
        _mockRepository.GetById(id).Returns(topUp);
        var sut = new GetTopUpByIdHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetTopUpByIdQuery(id), CancellationToken.None);

        // Assert
        result.Id.Should().Be(id);
        result.UserName.Should().Be("testUser");
        result.Saldo.Should().Be(100m);
    }

    [TestMethod]
    public async Task GetTopUpById_WithNonExistentId_ShouldPropagateNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Throws(new NotFoundException("TopUp not found"));
        var sut = new GetTopUpByIdHandler(_mockRepository);

        // Act
        Func<Task> action = () => sut.Handle(new GetTopUpByIdQuery(id), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("TopUp not found");
    }

    [TestMethod]
    public async Task GetTopUps_PassesTakeAndSkipInRepositoryOrderAndReturnsCount()
    {
        // Arrange
        _mockRepository.GetAll(15, 30).Returns(new List<TopUp> { CreateTopUp(Guid.NewGuid(), "a", 1m) });
        _mockRepository.GetCount().Returns(31);
        var sut = new GetTopUpsHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetTopUpsQuery(30, 15), CancellationToken.None);

        // Assert
        result.Items.Should().ContainSingle();
        result.TotalCount.Should().Be(31);
    }

    [TestMethod]
    public async Task GetPersonalTopUps_ReturnsUsersTopUpsAndCount()
    {
        // Arrange
        _mockRepository.GetPersonal("me", 10, 0).Returns(new List<TopUp> { CreateTopUp(Guid.NewGuid(), "me", 5m) });
        _mockRepository.GetPersonalCount("me").Returns(1);
        var sut = new GetPersonalTopUpsHandler(_mockRepository);

        // Act
        var result = await sut.Handle(new GetPersonalTopUpsQuery("me", 0, 10), CancellationToken.None);

        // Assert
        result.Items.Should().ContainSingle().Which.UserName.Should().Be("me");
        result.TotalCount.Should().Be(1);
    }

    #endregion

    #region CreateTopUp Tests

    [TestMethod]
    public async Task CreateTopUp_WithValidUser_ShouldCallRepositoryAndDispatchEvent()
    {
        // Arrange
        var user = new ApplicationUser { Id = "testUser-id", UserName = "testUser" };
        _mockUserRepository.GetByUsername("testUser").Returns(user);

        // Act
        var result = await CreateHandler().Handle(new CreateTopUpCommand("testUser", 100m), CancellationToken.None);

        // Assert
        await _mockRepository.Received(1).Create(Arg.Is<TopUp>(t => t.User == user && t.Saldo == 100m));
        await _mockEventDispatcher.Received(1).DispatchAsync(Arg.Is<TopUpEvent>(e => e.TopUp.User == user));
        result.UserName.Should().Be("testUser");
        result.Saldo.Should().Be(100m);
    }

    [TestMethod]
    public async Task CreateTopUp_WhenUserNotFound_ShouldPropagateNotFoundException()
    {
        // Arrange
        _mockUserRepository.GetByUsername("ghost").Throws(new NotFoundException("ApplicationUser not found"));

        // Act
        Func<Task> action = () => CreateHandler().Handle(new CreateTopUpCommand("ghost", 10m), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>();
        await _mockRepository.DidNotReceive().Create(Arg.Any<TopUp>());
    }

    [TestMethod]
    public async Task CreateTopUp_WithNegativeSaldo_ShouldStillCallRepository()
    {
        // Arrange
        _mockUserRepository.GetByUsername("testUser").Returns(new ApplicationUser { Id = "id", UserName = "testUser" });

        // Act
        await CreateHandler().Handle(new CreateTopUpCommand("testUser", -50m), CancellationToken.None);

        // Assert
        await _mockRepository.Received(1).Create(Arg.Is<TopUp>(t => t.Saldo == -50m));
    }

    [TestMethod]
    public async Task CreateTopUp_WhenRepositoryThrowsException_ShouldPropagateAndNotDispatch()
    {
        // Arrange
        _mockUserRepository.GetByUsername("testUser").Returns(new ApplicationUser { Id = "id", UserName = "testUser" });
        _mockRepository.Create(Arg.Any<TopUp>()).Throws(new InvalidOperationException("Failed to create top-up"));

        // Act
        Func<Task> action = () => CreateHandler().Handle(new CreateTopUpCommand("testUser", 100m), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Failed to create top-up");
        await _mockEventDispatcher.DidNotReceive().DispatchAsync(Arg.Any<TopUpEvent>());
    }

    #endregion

    #region DeleteTopUp Tests

    [TestMethod]
    public async Task DeleteTopUp_WithValidId_ShouldDeleteAndRecalculateStats()
    {
        // Arrange
        var id = Guid.NewGuid();
        var topUp = CreateTopUp(id, "testUser", 100m);
        _mockRepository.GetById(id).Returns(topUp);
        var sut = new DeleteTopUpHandler(_mockRepository, _mockUserStatRepository);

        // Act
        await sut.Handle(new DeleteTopUpCommand(id), CancellationToken.None);

        // Assert
        await _mockRepository.Received(1).Delete(id);
        await _mockUserStatRepository.Received(1).RecalculateByUserId(topUp.User.Id);
    }

    [TestMethod]
    public async Task DeleteTopUp_WhenTopUpDoesNotExist_ShouldPropagateNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Throws(new NotFoundException("TopUp not found"));
        var sut = new DeleteTopUpHandler(_mockRepository, _mockUserStatRepository);

        // Act
        Func<Task> action = () => sut.Handle(new DeleteTopUpCommand(id), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("TopUp not found");
        await _mockRepository.DidNotReceive().Delete(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task DeleteTopUp_WhenRepositoryDeleteThrows_ShouldPropagateException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.GetById(id).Returns(CreateTopUp(id, "testUser", 10m));
        _mockRepository.Delete(id).Throws(new InvalidOperationException("Database error"));
        var sut = new DeleteTopUpHandler(_mockRepository, _mockUserStatRepository);

        // Act
        Func<Task> action = () => sut.Handle(new DeleteTopUpCommand(id), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Database error");
        await _mockUserStatRepository.DidNotReceive().RecalculateByUserId(Arg.Any<string>());
    }

    #endregion

    private static TopUp CreateTopUp(Guid id, string username, decimal saldo)
    {
        return new TopUp
        {
            Id = id,
            User = new ApplicationUser { Id = $"{username}-id", UserName = username },
            Saldo = saldo,
            CreatedOn = DateTime.UtcNow
        };
    }
}
