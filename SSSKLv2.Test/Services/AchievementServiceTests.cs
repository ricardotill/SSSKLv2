using FluentAssertions;
using NSubstitute;
using SSSKLv2.Agents;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services;
using SSSKLv2.Dto;
using SSSKLv2.Services.Interfaces;
using System.IO;
using System.Net.Mime;

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
    private IPurchaseNotifier _purchaseNotifier = null!;
    private INotificationService _notificationService = null!;
    private IUserStatRepository _mockUserStatRepository = null!;
    
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
        _purchaseNotifier = Substitute.For<IPurchaseNotifier>();
        _notificationService = Substitute.For<INotificationService>();
        _mockUserStatRepository = Substitute.For<IUserStatRepository>();
        
        // Create the system under test
        _sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository,
            _applicationUserRepository, _purchaseNotifier, _blobStorageAgent, _notificationService, _mockUserStatRepository);
        
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

        _mockUserStatRepository.GetOrCreateByUserId(Arg.Any<string>()).Returns(callInfo => new UserStat { UserId = callInfo.Arg<string>() });
    }
    
    

    #region GetPersonalAchievementsByUsername & GetPersonalAchievementEntriesByUsername Tests

    [TestMethod]
    public async Task GetPersonalAchievementsByUsername_UserExists_ReturnsListWithCorrectCompletedFlags()
    {
        // Arrange
        var username = _testUser.UserName;
        var a1 = new Achievement { Id = Guid.NewGuid(), Name = "A1" };
        var a2 = new Achievement { Id = Guid.NewGuid(), Name = "A2" };
        var achievements = new List<Achievement> { a1, a2 };
        var entries = new List<AchievementEntry>
        {
            new AchievementEntry { Achievement = a1, User = _testUser }
        };

        _applicationUserRepository.GetByUsername(username!).Returns(_testUser);
        _achievementRepository.GetAll().Returns(achievements);
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(entries);

        // Act
        var result = await _sut.GetPersonalAchievementsByUsername(username!);

        // Assert
        result.Should().HaveCount(2);
        result.Single(a => a.Name == "A1").Completed.Should().BeTrue();
        result.Single(a => a.Name == "A2").Completed.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetPersonalAchievementsByUsername_UserNotFound_ReturnsEmptyList()
    {
        // Arrange
        var username = "nonexistent";
        _applicationUserRepository.GetByUsername(username).Returns(Task.FromResult<ApplicationUser>(null!));

        // Act
        var result = await _sut.GetPersonalAchievementsByUsername(username!);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetPersonalAchievementEntriesByUsername_UserExists_ReturnsEntries()
    {
        // Arrange
        var username = _testUser.UserName;
        var entries = new List<AchievementEntry> { new AchievementEntry { Id = Guid.NewGuid(), User = _testUser } };
        _applicationUserRepository.GetByUsername(username!).Returns(_testUser);
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(entries);

        // Act
        var result = await _sut.GetPersonalAchievementEntriesByUsername(username!);

        // Assert
        result.Should().BeEquivalentTo(entries);
    }

    [TestMethod]
    public async Task GetPersonalAchievementEntriesByUsername_UserNotFound_ReturnsEmptyList()
    {
        // Arrange
        var username = "no-user";
        _applicationUserRepository.GetByUsername(username).Returns(Task.FromResult<ApplicationUser>(null!));

        // Act
        var result = await _sut.GetPersonalAchievementEntriesByUsername(username!);

        // Assert
        result.Should().BeEmpty();
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
        var sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository, applicationUserRepository, _purchaseNotifier, _blobStorageAgent, _notificationService, _mockUserStatRepository);
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
        var sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository, applicationUserRepository, _purchaseNotifier, _blobStorageAgent, _notificationService, _mockUserStatRepository);
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
        var sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository, applicationUserRepository, _purchaseNotifier, _blobStorageAgent, _notificationService, _mockUserStatRepository);
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
        var sut = new AchievementService(_achievementRepository, _orderRepository, _topUpRepository, applicationUserRepository, _purchaseNotifier, _blobStorageAgent, _notificationService, _mockUserStatRepository);
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

    #region New Methods Tests

    [TestMethod]
    public async Task GetPersonalUnseenAchievementEntries_WhenUnseenExist_MarksAsSeenAndReturnsUpdated()
    {
        // Arrange
        var username = "user1";
        var entries = new List<AchievementEntry> 
        { 
            new AchievementEntry { Id = Guid.NewGuid(), Achievement = new Achievement { Id = Guid.NewGuid() }, HasSeen = false } 
        };
        _achievementRepository.GetPersonalUnseenAchievementEntries(username).Returns(entries);

        // Act
        var result = await _sut.GetPersonalUnseenAchievementEntries(username);

        // Assert
        result.Should().HaveCount(1);
        result.First().HasSeen.Should().BeTrue();
        await _achievementRepository.Received(1).UpdateAchievementEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(list => list.All(e => e.HasSeen)));
    }

    [TestMethod]
    public async Task GetEntriesForAchievement_ReturnsEntriesFromRepository()
    {
        // Arrange
        var achievementId = Guid.NewGuid();
        var entries = new List<AchievementEntry> { new AchievementEntry { Id = Guid.NewGuid() } };
        _achievementRepository.GetAllEntries(achievementId).Returns(entries);

        // Act
        var result = await _sut.GetEntriesForAchievement(achievementId);

        // Assert
        result.Should().BeEquivalentTo(entries);
    }

    [TestMethod]
    public async Task GetPersonalAchievementsByUsername_ResolvesUserAndCallsExistingLogic()
    {
        // Arrange
        var username = "user1";
        var user = new ApplicationUser { Id = "u1", UserName = username };
        _applicationUserRepository.GetByUsername(username).Returns(user);
        _achievementRepository.GetAll().Returns(new List<Achievement>());
        _achievementRepository.GetAllEntriesOfUser(user.Id).Returns(new List<AchievementEntry>());

        // Act
        var result = await _sut.GetPersonalAchievementsByUsername(username);

        // Assert
        result.Should().NotBeNull();
        await _applicationUserRepository.Received(1).GetByUsername(username);
        await _achievementRepository.Received(1).GetAllEntriesOfUser(user.Id);
    }

    [TestMethod]
    public async Task GetPersonalAchievementEntriesByUsername_ResolvesUserAndCallsExistingLogic()
    {
        // Arrange
        var username = "user1";
        var user = new ApplicationUser { Id = "u1", UserName = username };
        _applicationUserRepository.GetByUsername(username).Returns(user);
        _achievementRepository.GetAllEntriesOfUser(user.Id).Returns(new List<AchievementEntry>());

        // Act
        var result = await _sut.GetPersonalAchievementEntriesByUsername(username);

        // Assert
        result.Should().NotBeNull();
        await _applicationUserRepository.Received(1).GetByUsername(username);
        await _achievementRepository.Received(1).GetAllEntriesOfUser(user.Id);
    }
    [TestMethod]
    public async Task GetCount_ShouldReturnCountFromRepository()
    {
        _achievementRepository.GetCount().Returns(Task.FromResult(5));
        var result = await _sut.GetCount();
        result.Should().Be(5);
    }

    [TestMethod]
    public async Task GetAchievements_Paged_ShouldReturnPagedResults()
    {
        var achievements = new List<Achievement> { new Achievement { Id = Guid.NewGuid() } };
        _achievementRepository.GetAll(10, 5).Returns(Task.FromResult<IList<Achievement>>(achievements));
        var result = await _sut.GetAchievements(10, 5);
        result.Should().BeEquivalentTo(achievements);
    }

    [TestMethod]
    public async Task GetAchievementById_ShouldReturnAchievement()
    {
        var id = Guid.NewGuid();
        var achievement = new Achievement { Id = id };
        _achievementRepository.GetById(id).Returns(Task.FromResult(achievement));
        var result = await _sut.GetAchievementById(id);
        result.Should().Be(achievement);
    }

    [TestMethod]
    public async Task UpdateAchievement_ShouldCallRepository()
    {
        var achievement = new Achievement { Id = Guid.NewGuid() };
        await _sut.UpdateAchievement(achievement);
        await _achievementRepository.Received(1).Update(achievement);
    }

    [TestMethod]
    public async Task DeleteAchievement_ShouldCallRepository()
    {
        var id = Guid.NewGuid();
        await _sut.DeleteAchievement(id);
        await _achievementRepository.Received(1).Delete(id);
    }

    [TestMethod]
    public async Task DeleteAchievementEntryRange_ShouldCallRepository()
    {
        var entries = new List<AchievementEntry> { new AchievementEntry { Id = Guid.NewGuid() } };
        await _sut.DeleteAchievementEntryRange(entries);
        await _achievementRepository.Received(1).DeleteAchievementEntryRange(entries);
    }

    [TestMethod]
    public async Task AddAchievement_WithValidData_ShouldUploadImageAndCreateAchievement()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 0x01, 0x02 });
        var dto = new AchievementDto
        {
            Name = "New Achievement",
            Description = "Desc",
            ImageContent = stream,
            ImageContentType = new ContentType("image/png"),
            AutoAchieve = true,
            Action = Achievement.ActionOption.UserOrderAmountBought,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 10
        };

        var blobItem = new BlobStorageItem 
        { 
            Id = Guid.NewGuid(), 
            FileName = "test.png", 
            Uri = "http://test.com/test.png",
            ContentType = "image/png",
            CreatedOn = DateTime.Now
        };
        _blobStorageAgent.UploadFileToBlobAsync(Arg.Any<string>(), "image/png", stream).Returns(blobItem);

        // Act
        await _sut.AddAchievement(dto);

        // Assert
        await _blobStorageAgent.Received(1).UploadFileToBlobAsync(Arg.Any<string>(), "image/png", stream);
        await _achievementRepository.Received(1).Create(Arg.Is<Achievement>(a => 
            a.Name == dto.Name && 
            a.Description == dto.Description &&
            a.Image != null &&
            a.Image.FileName == "test.png"
        ));
    }

    [TestMethod]
    public async Task AddAchievement_MissingImage_ShouldThrowArgumentException()
    {
        var dto = new AchievementDto { Name = "Fail", Description = "Fail" };
        var act = () => _sut.AddAchievement(dto);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
