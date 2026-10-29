using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SSSKL.Application.Features.Leaderboards.Queries.GetLeaderboard;
using SSSKL.Application.Features.Users;
using SSSKL.Application.Features.Users.Commands.DeleteProfilePicture;
using SSSKL.Application.Features.Users.Commands.DeleteUser;
using SSSKL.Application.Features.Users.Commands.UpdateProfilePicture;
using SSSKL.Application.Features.Users.Commands.UpdateUser;
using SSSKL.Application.Features.Users.Queries.GetUserDetails;
using SSSKL.Application.Features.Users.Queries.GetUserProfile;
using SSSKL.Application.Features.Users.Queries.GetUsers;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Agents;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Test.Features.Users;

[TestClass]
public class UserHandlerTests
{
    private IApplicationUserRepository _mockUserRepository = null!;
    private IProductRepository _mockProductRepository = null!;
    private IProductUserStatRepository _mockProductUserStatRepository = null!;
    private IOrderRepository _mockOrderRepository = null!;
    private IMemoryCache _cache = null!;
    private UserManager<ApplicationUser> _fakeUserManager = null!;
    private IBlobStorageAgent _mockBlobAgent = null!;
    private IUserImageRepository _mockUserImageRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockUserRepository = Substitute.For<IApplicationUserRepository>();
        _mockProductRepository = Substitute.For<IProductRepository>();
        _mockProductUserStatRepository = Substitute.For<IProductUserStatRepository>();
        _mockOrderRepository = Substitute.For<IOrderRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Create a very lightweight fake UserManager by providing a fake store and dependencies
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        var options = Options.Create(new IdentityOptions());
        var pwdHasher = Substitute.For<IPasswordHasher<ApplicationUser>>();
        var userValidators = new List<IUserValidator<ApplicationUser>>();
        var pwdValidators = new List<IPasswordValidator<ApplicationUser>>();
        var lookupNormalizer = Substitute.For<ILookupNormalizer>();
        var describer = new IdentityErrorDescriber();
        var services = Substitute.For<IServiceProvider>();
        var umLogger = Substitute.For<ILogger<UserManager<ApplicationUser>>>();

        _fakeUserManager = new FakeUserManager(store, options, pwdHasher, userValidators, pwdValidators, lookupNormalizer, describer, services, umLogger);
        _mockBlobAgent = Substitute.For<IBlobStorageAgent>();
        
        _mockUserImageRepository = Substitute.For<IUserImageRepository>();


        // Like the real repository, look users up in the same store as the UserManager and throw when missing
        _mockUserRepository.GetById(Arg.Any<string>()).Returns(ci =>
            ((FakeUserManager)_fakeUserManager).FindByIdFunc(ci.Arg<string>()).Result is { } user
                ? Task.FromResult(user)
                : Task.FromException<ApplicationUser>(new NotFoundException("ApplicationUser not found")));
    }

    // The tests below were written against ApplicationUserService; these helpers send the same calls to the handlers.

    private Task<IReadOnlyList<LeaderboardEntryDto>> Leaderboard(Guid productId, LeaderboardPeriod period) =>
        new GetLeaderboardHandler(_mockProductRepository, _mockProductUserStatRepository, _mockOrderRepository, _mockUserRepository, _cache)
            .Handle(new GetLeaderboardQuery(productId, period), CancellationToken.None);

    private Task<IReadOnlyList<LeaderboardEntryDto>> GetAllLeaderboard(Guid productId) => Leaderboard(productId, LeaderboardPeriod.All);
    private Task<IReadOnlyList<LeaderboardEntryDto>> GetMonthlyLeaderboard(Guid productId) => Leaderboard(productId, LeaderboardPeriod.Monthly);
    private Task<IReadOnlyList<LeaderboardEntryDto>> Get12HourlyLeaderboard(Guid productId) => Leaderboard(productId, LeaderboardPeriod.TwelveHours);
    private Task<IReadOnlyList<LeaderboardEntryDto>> Get12HourlyLiveLeaderboard(Guid productId) => Leaderboard(productId, LeaderboardPeriod.TwelveHoursLive);

    private Task<ApplicationUserDetailedDto> UpdateUser(string id, ApplicationUserUpdateDto dto) =>
        new UpdateUserHandler(_mockUserRepository, _fakeUserManager)
            .Handle(new UpdateUserCommand(UserReference.ById(id), dto, null, IsAdmin: true), CancellationToken.None);

    private Task DeleteUser(string id) =>
        new DeleteUserHandler(_mockUserRepository, _fakeUserManager).Handle(new DeleteUserCommand(UserReference.ById(id)), CancellationToken.None);

    private Task<IList<string>> GetUserRoles(string id) => new UserRoleProvider(_fakeUserManager).GetRoles(id);

    private Task<PaginationObject<ApplicationUserDto>> GetUsers(int skip, int take, bool forAdmin = false) =>
        new GetUsersHandler(_mockUserRepository).Handle(new GetUsersQuery(skip, take, forAdmin), CancellationToken.None);

    private Task UpdateProfilePictureAsync(string userId, Stream stream) =>
        new UpdateProfilePictureHandler(_mockUserRepository, _fakeUserManager, _mockUserImageRepository, _mockBlobAgent)
            .Handle(new UpdateProfilePictureCommand(UserReference.ById(userId), stream), CancellationToken.None);

    private Task DeleteProfilePictureAsync(string userId) =>
        new DeleteProfilePictureHandler(_mockUserRepository, _fakeUserManager, _mockUserImageRepository, _mockBlobAgent)
            .Handle(new DeleteProfilePictureCommand(UserReference.ById(userId)), CancellationToken.None);

    #region User lookup Tests

    [TestMethod]
    public async Task GetUserProfile_WithValidId_ReturnsProfile()
    {
        // Arrange
        var userId = "user-123";
        var expectedUser = CreateApplicationUser(userId);
        _mockUserRepository.GetById(userId).Returns(expectedUser);
        var sut = new GetUserProfileHandler(_mockUserRepository);

        // Act
        var result = await sut.Handle(new GetUserProfileQuery(userId), CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedUser.ToDto());
        await _mockUserRepository.Received(1).GetById(userId);
    }

    [TestMethod]
    public async Task GetUserProfile_WithInvalidId_PropagatesNotFoundException()
    {
        // Arrange
        var userId = "non-existent-id";
        _mockUserRepository.GetById(userId).Returns(Task.FromException<ApplicationUser>(new NotFoundException("User not found")));
        var sut = new GetUserProfileHandler(_mockUserRepository);

        // Act
        Func<Task> action = () => sut.Handle(new GetUserProfileQuery(userId), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("User not found");
    }

    [TestMethod]
    public async Task GetUserDetails_ByUsername_ReturnsUserWithRoles()
    {
        // Arrange
        var username = "testuser";
        var expectedUser = CreateApplicationUser("user-123", username);
        _mockUserRepository.GetByUsername(username).Returns(expectedUser);
        var roleProvider = Substitute.For<IUserRoleProvider>();
        roleProvider.GetRoles("user-123").Returns(new List<string> { Roles.User });
        var sut = new GetUserDetailsHandler(_mockUserRepository, roleProvider);

        // Act
        var result = await sut.Handle(new GetUserDetailsQuery(UserReference.ByUsername(username)), CancellationToken.None);

        // Assert
        result.Id.Should().Be("user-123");
        result.UserName.Should().Be(username);
        result.Email.Should().Be(expectedUser.Email);
        result.Roles.Should().Equal(Roles.User);
    }

    [TestMethod]
    public async Task GetUserDetails_WithInvalidUsername_PropagatesNotFoundException()
    {
        // Arrange
        var username = "non-existent-user";
        _mockUserRepository.GetByUsername(username).Returns(Task.FromException<ApplicationUser>(new NotFoundException("User not found")));
        var sut = new GetUserDetailsHandler(_mockUserRepository, Substitute.For<IUserRoleProvider>());

        // Act
        Func<Task> action = () => sut.Handle(new GetUserDetailsQuery(UserReference.ByUsername(username)), CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<NotFoundException>().WithMessage("User not found");
    }

    [TestMethod]
    public async Task UpdateUser_AsNonOwnerNonAdmin_ThrowsUnauthorized()
    {
        // Arrange
        var existing = CreateApplicationUser("user1", "owner");
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(existing);
        var sut = new UpdateUserHandler(_mockUserRepository, _fakeUserManager);

        // Act
        Func<Task> act = () => sut.Handle(new UpdateUserCommand(UserReference.ById("user1"), new ApplicationUserUpdateDto { Name = "x" }, "someone-else", IsAdmin: false), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task UpdateUser_AsOwner_UpdatesProfile()
    {
        // Arrange
        var existing = CreateApplicationUser("user1", "owner");
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(existing);
        var sut = new UpdateUserHandler(_mockUserRepository, _fakeUserManager);

        // Act
        var result = await sut.Handle(new UpdateUserCommand(UserReference.ById("user1"), new ApplicationUserUpdateDto { Name = "Nieuw" }, "OWNER", IsAdmin: false), CancellationToken.None);

        // Assert
        result.Name.Should().Be("Nieuw");
    }

    #endregion

    #region GetAllLeaderboard Tests

    [TestMethod]
    public async Task GetAllLeaderboard_WithValidProductId_ReturnsLeaderboard()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        var stats = CreateProductUserStats(productId);
        
        _mockProductRepository.GetById(productId).Returns(product!);
        _mockProductUserStatRepository.GetAllForProduct(productId).Returns(stats);

        // Act
        var result = await GetAllLeaderboard(productId);
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
        await _mockProductUserStatRepository.Received(1).GetAllForProduct(productId);
    }

    [TestMethod]
    public async Task GetAllLeaderboard_WithOverflowAmount_HandlesOverflow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        
        // Simulate a cached stat row where the amount is already saturated at int.MaxValue
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        var stat = new ProductUserStat
        {
            UserId = user.Id,
            User = user,
            ProductId = productId,
            TotalAmount = int.MaxValue
        };
        
        _mockProductRepository.GetById(productId).Returns(product!);
        _mockProductUserStatRepository.GetAllForProduct(productId).Returns(new List<ProductUserStat> { stat });

        // Act
        var result = await GetAllLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(1);
        resultList[0].Amount.Should().Be(int.MaxValue);
        resultList[0].Position.Should().Be(1);
        
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockProductUserStatRepository.Received(1).GetAllForProduct(productId);
    }

    [TestMethod]
    public async Task GetAllLeaderboard_WithNoQualifyingOrders_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        
        _mockProductRepository.GetById(productId).Returns(product!);
        _mockProductUserStatRepository.GetAllForProduct(productId).Returns(new List<ProductUserStat>());

        // Act
        var result = await GetAllLeaderboard(productId);

        // Assert
        result.Should().BeEmpty();
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockProductUserStatRepository.Received(1).GetAllForProduct(productId);
    }

    #endregion

    #region GetMonthlyLeaderboard Tests

    [TestMethod]
    public async Task GetMonthlyLeaderboard_WithValidProductId_ReturnsMonthlyLeaderboard()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");

        var user1 = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        var user2 = CreateApplicationUser("user-2", "user2", "Test2", "User2");

        var aggregates = new List<OrderAggregate>
        {
            new OrderAggregate(user1.Id, 5),
            new OrderAggregate(user2.Id, 3)
        };

        _mockProductRepository.GetById(productId).Returns(product!);
        _mockOrderRepository.GetAmountAggregates(productId, Arg.Any<DateTime>(), Arg.Any<DateTime?>(), null).Returns(aggregates);
        _mockUserRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { user1, user2 });

        // Act
        var result = await GetMonthlyLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(2);
        resultList[0].Position.Should().Be(1);
        resultList[0].Amount.Should().Be(5);
        resultList[1].Position.Should().Be(2);
        resultList[1].Amount.Should().Be(3);

        await _mockProductRepository.Received(1).GetById(productId);
    }

    [TestMethod]
    public async Task GetMonthlyLeaderboard_WithOverflowAmount_HandlesOverflow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");

        // SQL-side sum uses long, so an over-int totals is represented exactly here
        var aggregates = new List<OrderAggregate> { new OrderAggregate(user.Id, (long)int.MaxValue * 2) };

        _mockProductRepository.GetById(productId).Returns(product!);
        _mockOrderRepository.GetAmountAggregates(productId, Arg.Any<DateTime>(), Arg.Any<DateTime?>(), null).Returns(aggregates);
        _mockUserRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { user });

        // Act
        var result = await GetMonthlyLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(1);
        resultList[0].Amount.Should().Be(int.MaxValue); // Amount should be capped at int.MaxValue
        resultList[0].Position.Should().Be(1);
    }

    [TestMethod]
    public async Task GetMonthlyLeaderboard_WithNoQualifyingOrders_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");

        _mockProductRepository.GetById(productId).Returns(product!);
        _mockOrderRepository.GetAmountAggregates(productId, Arg.Any<DateTime>(), Arg.Any<DateTime?>(), null).Returns(new List<OrderAggregate>());

        // Act
        var result = await GetMonthlyLeaderboard(productId);

        // Assert
        result.Should().BeEmpty();
        await _mockProductRepository.Received(1).GetById(productId);
    }

    #endregion

    #region Get12HourlyLeaderboard Tests

    [TestMethod]
    public async Task Get12HourlyLeaderboard_WithValidProductId_Returns12HourLeaderboard()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");

        var user1 = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        var user2 = CreateApplicationUser("user-2", "user2", "Test2", "User2");

        var aggregates = new List<OrderAggregate>
        {
            new OrderAggregate(user1.Id, 5),
            new OrderAggregate(user2.Id, 8)
        };

        _mockProductRepository.GetById(productId).Returns(product!);
        _mockOrderRepository.GetAmountAggregates(productId, Arg.Any<DateTime>(), null, null).Returns(aggregates);
        _mockUserRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { user1, user2 });

        // Act
        var result = await Get12HourlyLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(2);
        resultList[0].Position.Should().Be(1);
        resultList[0].Amount.Should().Be(8); // User2 has more orders in the last 12 hours
        resultList[1].Position.Should().Be(2);
        resultList[1].Amount.Should().Be(5);

        await _mockProductRepository.Received(1).GetById(productId);
    }

    [TestMethod]
    public async Task Get12HourlyLeaderboard_WithOverflowAmount_HandlesOverflow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");

        var aggregates = new List<OrderAggregate> { new OrderAggregate(user.Id, (long)int.MaxValue * 2) };

        _mockProductRepository.GetById(productId).Returns(product!);
        _mockOrderRepository.GetAmountAggregates(productId, Arg.Any<DateTime>(), null, null).Returns(aggregates);
        _mockUserRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { user });

        // Act
        var result = await Get12HourlyLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(1);
        resultList[0].Amount.Should().Be(int.MaxValue); // Amount should be capped at int.MaxValue
        resultList[0].Position.Should().Be(1);
    }

    [TestMethod]
    public async Task Get12HourlyLeaderboard_WithNoQualifyingOrders_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");

        _mockProductRepository.GetById(productId).Returns(product!);
        _mockOrderRepository.GetAmountAggregates(productId, Arg.Any<DateTime>(), null, null).Returns(new List<OrderAggregate>());

        // Act
        var result = await Get12HourlyLeaderboard(productId);

        // Assert
        result.Should().BeEmpty();
        await _mockProductRepository.Received(1).GetById(productId);
    }

    #endregion

    #region Get12HourlyLiveLeaderboard Tests

    [TestMethod]
    public async Task Get12HourlyLiveLeaderboard_WithValidProductId_Returns12HourLiveLeaderboard()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");

        var user1 = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        var user2 = CreateApplicationUser("user-2", "user2", "Test2", "User2");

        var aggregates = new List<OrderAggregate>
        {
            new OrderAggregate(user1.Id, 5),
            new OrderAggregate(user2.Id, 8)
        };

        _mockProductRepository.GetById(productId).Returns(product!);
        _mockUserRepository.GetTopActiveUserIds(10).Returns(new List<string> { user1.Id, user2.Id });
        _mockOrderRepository.GetAmountAggregates(productId, Arg.Any<DateTime>(), null, Arg.Any<IEnumerable<string>>()).Returns(aggregates);
        _mockUserRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { user1, user2 });

        // Act
        var result = await Get12HourlyLiveLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(2);
        resultList[0].Position.Should().Be(1);
        resultList[0].Amount.Should().Be(8); // User2 has more orders in the last 12 hours
        resultList[1].Position.Should().Be(2);
        resultList[1].Amount.Should().Be(5);

        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetTopActiveUserIds(10);
    }

    [TestMethod]
    public async Task Get12HourlyLiveLeaderboard_WithOverflowAmount_HandlesOverflow()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");
        var user = CreateApplicationUser("user-1", "user1", "Test1", "User1");

        var aggregates = new List<OrderAggregate> { new OrderAggregate(user.Id, (long)int.MaxValue * 2) };

        _mockProductRepository.GetById(productId).Returns(product!);
        _mockUserRepository.GetTopActiveUserIds(10).Returns(new List<string> { user.Id });
        _mockOrderRepository.GetAmountAggregates(productId, Arg.Any<DateTime>(), null, Arg.Any<IEnumerable<string>>()).Returns(aggregates);
        _mockUserRepository.GetByIds(Arg.Any<IEnumerable<string>>()).Returns(new List<ApplicationUser> { user });

        // Act
        var result = await Get12HourlyLiveLeaderboard(productId);
        var resultList = result.ToList();

        // Assert
        resultList.Should().HaveCount(1);
        resultList[0].Amount.Should().Be(int.MaxValue); // Amount should be capped at int.MaxValue
        resultList[0].Position.Should().Be(1);
    }

    [TestMethod]
    public async Task Get12HourlyLiveLeaderboard_WithNoQualifyingOrders_ReturnsEmptyList()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId, "Test Product");

        _mockProductRepository.GetById(productId).Returns(product!);
        _mockUserRepository.GetTopActiveUserIds(10).Returns(new List<string>());

        // Act
        var result = await Get12HourlyLiveLeaderboard(productId);

        // Assert
        result.Should().BeEmpty();
        await _mockProductRepository.Received(1).GetById(productId);
        await _mockUserRepository.Received(1).GetTopActiveUserIds(10);
    }

    #endregion

    #region DeterminePositions Tests

    [TestMethod]
    public void DeterminePositions_OrdersEntriesByAmountDescending()
    {
        // Arrange
        var entries = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Amount = 5, FullName = "User1", ProductName = "Product" },
            new LeaderboardEntryDto { Amount = 10, FullName = "User2", ProductName = "Product" },
            new LeaderboardEntryDto { Amount = 3, FullName = "User3", ProductName = "Product" }
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
        var entries = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Amount = 5, FullName = "User1", ProductName = "Product" },
            new LeaderboardEntryDto { Amount = 10, FullName = "User2", ProductName = "Product" },
            new LeaderboardEntryDto { Amount = 5, FullName = "User3", ProductName = "Product" }
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
        var entries = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Amount = 5, FullName = "User1", ProductName = "Product" },
            new LeaderboardEntryDto { Amount = 0, FullName = "User2", ProductName = "Product" },
            new LeaderboardEntryDto { Amount = 3, FullName = "User3", ProductName = "Product" }
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
        var entries = new List<LeaderboardEntryDto>();

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

    private List<ProductUserStat> CreateProductUserStats(Guid productId)
    {
        var user1 = CreateApplicationUser("user-1", "user1", "Test1", "User1");
        var user2 = CreateApplicationUser("user-2", "user2", "Test2", "User2");

        return new List<ProductUserStat>
        {
            new ProductUserStat { UserId = user1.Id, User = user1, ProductId = productId, TotalAmount = 5 },
            new ProductUserStat { UserId = user2.Id, User = user2, ProductId = productId, TotalAmount = 3 }
        };
    }



    // Helper method to invoke private DeterminePositions method using reflection
    private IEnumerable<LeaderboardEntryDto> InvokeDeterminePositions(IEnumerable<LeaderboardEntryDto> entries)
    {
        var methodInfo = typeof(GetLeaderboardHandler).GetMethod(
            "DeterminePositions", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        return (IEnumerable<LeaderboardEntryDto>)methodInfo!.Invoke(null, new object[] { entries })!;
    }

    #endregion

    [TestMethod]
    public async Task UpdateUser_WhenUserNotFound_ThrowsNotFound()
    {
        var id = "missing";
        // configure fake to return null
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = (uid) => Task.FromResult<ApplicationUser?>(null);

        var dto = new ApplicationUserUpdateDto { UserName = "x" };

        Func<Task> act = async () => await UpdateUser(id, dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task UpdateUser_WhenSetUserNameFails_ThrowsInvalidOperation()
    {
        var id = "user1";
        var existing = CreateApplicationUser(id, "old");
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = (uid) => Task.FromResult<ApplicationUser?>(existing);
        ((FakeUserManager)_fakeUserManager).SetUserNameFunc = (user, name) => Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Invalid" }));

        var dto = new ApplicationUserUpdateDto { UserName = "new" };

        Func<Task> act = async () => await UpdateUser(id, dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public async Task UpdateUser_WhenValid_UpdatesAndReturnsUser()
    {
        var id = "user1";
        var existing = CreateApplicationUser(id, "old", "Test", "User", "old@example.com");
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = (uid) => Task.FromResult<ApplicationUser?>(existing);
        ((FakeUserManager)_fakeUserManager).SetUserNameFunc = (user, name) => Task.FromResult(IdentityResult.Success);
        ((FakeUserManager)_fakeUserManager).SetEmailFunc = (user, email) => Task.FromResult(IdentityResult.Success);
        ((FakeUserManager)_fakeUserManager).SetPhoneFunc = (user, phone) => Task.FromResult(IdentityResult.Success);
        ((FakeUserManager)_fakeUserManager).UpdateFunc = (user) => Task.FromResult(IdentityResult.Success);
        ((FakeUserManager)_fakeUserManager).GeneratePasswordResetTokenFunc = (user) => Task.FromResult("token");
        ((FakeUserManager)_fakeUserManager).ResetPasswordFunc = (user, token, pwd) => Task.FromResult(IdentityResult.Success);
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = (uid) => Task.FromResult<ApplicationUser?>(existing);

        var dto = new ApplicationUserUpdateDto { UserName = "newname", Email = "new@example.com", PhoneNumber = "123", Name = "New", Surname = "Name", Password = "newpass" };

        var result = await UpdateUser(id, dto);

        result.Should().NotBeNull();
        result.UserName.Should().Be("newname");
        result.Email.Should().Be("new@example.com");
        result.PhoneNumber.Should().Be("123");
        result.Name.Should().Be("New");
        result.Surname.Should().Be("Name");
    }

    [TestMethod]
    public async Task UpdateUser_WhenDescriptionProvided_UpdatesDescription()
    {
        var id = "user1";
        var existing = CreateApplicationUser(id, "old", "Test", "User", "old@example.com");
        existing.Description = "Old Description";
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = (uid) => Task.FromResult<ApplicationUser?>(existing);
        ((FakeUserManager)_fakeUserManager).UpdateFunc = (user) => Task.FromResult(IdentityResult.Success);

        var dto = new ApplicationUserUpdateDto { Description = "New Description" };

        var result = await UpdateUser(id, dto);

        result.Should().NotBeNull();
        result.Description.Should().Be("New Description");
    }

    [TestMethod]
    public async Task DeleteUser_WhenUserNotFound_ThrowsNotFound()
    {
        var id = "missing";
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(null);

        Func<Task> act = async () => await DeleteUser(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task DeleteUser_WhenDeleteFails_ThrowsInvalidOperation()
    {
        var id = "user1";
        var user = CreateApplicationUser(id);
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(user);
        ((FakeUserManager)_fakeUserManager).DeleteFunc = _ => Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Cannot delete" }));

        Func<Task> act = async () => await DeleteUser(id);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public async Task DeleteUser_WhenValid_DeletesSuccessfully()
    {
        var id = "user1";
        var user = CreateApplicationUser(id);
        var deleteInvoked = false;
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(user);
        ((FakeUserManager)_fakeUserManager).DeleteFunc = _ =>
        {
            deleteInvoked = true;
            return Task.FromResult(IdentityResult.Success);
        };

        await DeleteUser(id);

        deleteInvoked.Should().BeTrue();
    }

    [TestMethod]
    public async Task UpdateUser_WhenRolesProvided_UpdatesRoles()
    {
        var id = "user1";
        var existing = CreateApplicationUser(id, "user1", "Test", "User", "test@example.com");
        var oldRoles = new List<string> { "OldRole" };
        var newRoles = new List<string> { "Admin", "Moderator" };

        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(existing);
        ((FakeUserManager)_fakeUserManager).GetRolesFunc = _ => Task.FromResult<IList<string>>(oldRoles);
        ((FakeUserManager)_fakeUserManager).RemoveFromRolesFunc = (_, roles) => Task.FromResult(IdentityResult.Success);
        ((FakeUserManager)_fakeUserManager).AddToRolesFunc = (_, roles) => Task.FromResult(IdentityResult.Success);
        ((FakeUserManager)_fakeUserManager).UpdateFunc = _ => Task.FromResult(IdentityResult.Success);
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(existing);

        var dto = new ApplicationUserUpdateDto { Roles = newRoles };

        var result = await UpdateUser(id, dto);

        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task UpdateUser_WhenRoleRemovalFails_ThrowsInvalidOperation()
    {
        var id = "user1";
        var existing = CreateApplicationUser(id);
        var oldRoles = new List<string> { "OldRole" };
        var newRoles = new List<string> { "Admin" };

        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(existing);
        ((FakeUserManager)_fakeUserManager).GetRolesFunc = _ => Task.FromResult<IList<string>>(oldRoles);
        ((FakeUserManager)_fakeUserManager).RemoveFromRolesFunc = (_, roles) => Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Cannot remove role" }));

        var dto = new ApplicationUserUpdateDto { Roles = newRoles };

        Func<Task> act = async () => await UpdateUser(id, dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public async Task UpdateUser_WhenRoleAdditionFails_ThrowsInvalidOperation()
    {
        var id = "user1";
        var existing = CreateApplicationUser(id);
        var oldRoles = new List<string> { "OldRole" };
        var newRoles = new List<string> { "Admin" };

        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(existing);
        ((FakeUserManager)_fakeUserManager).GetRolesFunc = _ => Task.FromResult<IList<string>>(oldRoles);
        ((FakeUserManager)_fakeUserManager).RemoveFromRolesFunc = (_, roles) => Task.FromResult(IdentityResult.Success);
        ((FakeUserManager)_fakeUserManager).AddToRolesFunc = (_, roles) => Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Cannot add role" }));

        var dto = new ApplicationUserUpdateDto { Roles = newRoles };

        Func<Task> act = async () => await UpdateUser(id, dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #region Paged and Count Tests

    [TestMethod]
    public async Task GetUsers_Paged_ShouldReturnUsersAndCountFromRepository()
    {
        var users = new List<ApplicationUser> { CreateApplicationUser("u1") };
        _mockUserRepository.GetAllPaged(10, 5).Returns(users);
        _mockUserRepository.GetCount().Returns(42);

        var result = await GetUsers(10, 5);

        result.Items.Should().ContainSingle().Which.Id.Should().Be("u1");
        result.TotalCount.Should().Be(42);
    }

    [TestMethod]
    public async Task GetUsers_ForAdmin_ShouldReturnAllUsersAndAdminCount()
    {
        var users = new List<ApplicationUser> { CreateApplicationUser("u1") };
        _mockUserRepository.GetAllForAdminPaged(10, 5).Returns(users);
        _mockUserRepository.GetCountAll().Returns(100);

        var result = await GetUsers(10, 5, forAdmin: true);

        result.Items.Should().ContainSingle().Which.Id.Should().Be("u1");
        result.TotalCount.Should().Be(100);
        await _mockUserRepository.DidNotReceive().GetAllPaged(Arg.Any<int>(), Arg.Any<int>());
    }

    [TestMethod]
    public async Task GetUserRoles_ShouldReturnRolesFromUserManager()
    {
        var id = "user1";
        var user = CreateApplicationUser(id);
        var roles = new List<string> { "Role1", "Role2" };
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(user);
        ((FakeUserManager)_fakeUserManager).GetRolesFunc = _ => Task.FromResult<IList<string>>(roles);

        var result = await GetUserRoles(id);
        result.Should().BeEquivalentTo(roles);
    }

    [TestMethod]
    public async Task GetUserRoles_WhenUserMissing_ThrowsNotFound()
    {
        var act = () => GetUserRoles("missing");
        await act.Should().ThrowAsync<NotFoundException>();
    }

    #endregion

    #region Profile Picture Tests

    [TestMethod]
    public async Task UpdateProfilePictureAsync_WithValidImage_ShouldUploadAndStore()
    {
        // Arrange
        var userId = "user1";
        var user = CreateApplicationUser(userId);

        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(user);
        
        // 1x1 transparent PNG
        var pngBytes = new byte[] { 
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, 
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x08, 0xD7, 0x63, 0x60, 0x00, 0x00, 0x00, 
            0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, 0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE, 
            0x42, 0x60, 0x82 
        };
        using var stream = new MemoryStream(pngBytes);
        
        var blobItem = new BlobStorageItem { Id = Guid.NewGuid(), FileName = "test.png", Uri = "http://test.com/test.png" };
        _mockBlobAgent.UploadFileToBlobAsync(Arg.Any<string>(), "image/png", Arg.Any<Stream>()).Returns(blobItem);

        // Act
        await UpdateProfilePictureAsync(userId, stream);

        // Assert
        user.ProfileImageId.Should().Be(blobItem.Id);
        await _mockUserImageRepository.Received(1).Add(Arg.Is<UserImage>(i => i.Id == blobItem.Id && i.FileName == "test.png"));
        await _mockBlobAgent.Received(1).UploadFileToBlobAsync(Arg.Any<string>(), "image/png", Arg.Any<Stream>());
    }

    [TestMethod]
    public async Task DeleteProfilePictureAsync_WithExistingPicture_ShouldCleanup()
    {
        // Arrange
        var userId = "user1";
        var user = CreateApplicationUser(userId);
        var image = new UserImage { Id = Guid.NewGuid(), FileName = "old.png", User = user };
        user.ProfileImage = image;
        user.ProfileImageId = image.Id;
        
        ((FakeUserManager)_fakeUserManager).FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(user);
        _mockUserImageRepository.GetById(image.Id).Returns(image);

        // Act
        await DeleteProfilePictureAsync(userId);

        // Assert
        user.ProfileImageId.Should().BeNull();
        await _mockUserImageRepository.Received(1).Delete(image);
        await _mockBlobAgent.Received(1).DeleteFileToBlobAsync("old.png");
    }

    #endregion

    // Minimal fake UserManager to allow per-test behavior via Func fields
    private class FakeUserManager : UserManager<ApplicationUser>
    {
        public Func<string, Task<ApplicationUser?>> FindByIdFunc = _ => Task.FromResult<ApplicationUser?>(null);
        public Func<ApplicationUser, string?, Task<IdentityResult>> SetUserNameFunc = (_, __) => Task.FromResult(IdentityResult.Success);
        public Func<ApplicationUser, string?, Task<IdentityResult>> SetEmailFunc = (_, __) => Task.FromResult(IdentityResult.Success);
        public Func<ApplicationUser, string?, Task<IdentityResult>> SetPhoneFunc = (_, __) => Task.FromResult(IdentityResult.Success);
        public Func<ApplicationUser, Task<IdentityResult>> UpdateFunc = _ => Task.FromResult(IdentityResult.Success);
        public Func<ApplicationUser, Task<IdentityResult>> DeleteFunc = _ => Task.FromResult(IdentityResult.Success);
        public Func<ApplicationUser, Task<IList<string>>> GetRolesFunc = _ => Task.FromResult<IList<string>>(new List<string>());
        public Func<ApplicationUser, IEnumerable<string>, Task<IdentityResult>> RemoveFromRolesFunc = (_, __) => Task.FromResult(IdentityResult.Success);
        public Func<ApplicationUser, IEnumerable<string>, Task<IdentityResult>> AddToRolesFunc = (_, __) => Task.FromResult(IdentityResult.Success);
        public Func<ApplicationUser, Task<string>> GeneratePasswordResetTokenFunc = _ => Task.FromResult(string.Empty);
        public Func<ApplicationUser, string, string, Task<IdentityResult>> ResetPasswordFunc = (_, __, ___) => Task.FromResult(IdentityResult.Success);

        public FakeUserManager(IUserStore<ApplicationUser> store, IOptions<IdentityOptions> optionsAccessor, IPasswordHasher<ApplicationUser> passwordHasher, IEnumerable<IUserValidator<ApplicationUser>> userValidators, IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators, ILookupNormalizer keyNormalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<ApplicationUser>> logger)
            : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
        {
        }

        public override Task<ApplicationUser?> FindByIdAsync(string userId) => FindByIdFunc(userId);
        public override Task<IdentityResult> SetUserNameAsync(ApplicationUser user, string? userName) => SetUserNameFunc(user, userName);
        public override Task<IdentityResult> SetEmailAsync(ApplicationUser user, string? email) => SetEmailFunc(user, email);
        public override Task<IdentityResult> SetPhoneNumberAsync(ApplicationUser user, string? phoneNumber) => SetPhoneFunc(user, phoneNumber);
        public override Task<IdentityResult> UpdateAsync(ApplicationUser user) => UpdateFunc(user);
        public override Task<IdentityResult> DeleteAsync(ApplicationUser user) => DeleteFunc(user);
        public override Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user) => GeneratePasswordResetTokenFunc(user);
        public override Task<IdentityResult> ResetPasswordAsync(ApplicationUser user, string token, string newPassword) => ResetPasswordFunc(user, token, newPassword);
        public override Task<IList<string>> GetRolesAsync(ApplicationUser user) => GetRolesFunc(user);
        public override Task<IdentityResult> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles) => RemoveFromRolesFunc(user, roles);
        public override Task<IdentityResult> AddToRolesAsync(ApplicationUser user, IEnumerable<string> roles) => AddToRolesFunc(user, roles);
    }
}

