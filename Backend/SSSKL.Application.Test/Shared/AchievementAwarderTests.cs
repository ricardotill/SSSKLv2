using FluentAssertions;
using NSubstitute;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Test.Shared;

[TestClass]
public class AchievementAwarderTests
{
    private IAchievementRepository _achievementRepository = null!;
    private IApplicationUserRepository _applicationUserRepository = null!;
    private IPurchaseNotifier _purchaseNotifier = null!;
    private IUserNotifier _userNotifier = null!;
    private AchievementAwarder _sut = null!;
    private ApplicationUser _testUser = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _achievementRepository = Substitute.For<IAchievementRepository>();
        _applicationUserRepository = Substitute.For<IApplicationUserRepository>();
        _purchaseNotifier = Substitute.For<IPurchaseNotifier>();
        _userNotifier = Substitute.For<IUserNotifier>();
        _sut = new AchievementAwarder(_achievementRepository, _applicationUserRepository, _purchaseNotifier, _userNotifier);

        _testUser = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Name = "Test",
            Surname = "User",
            Email = "test@example.com"
        };
        _applicationUserRepository.GetById(_testUser.Id).Returns(_testUser);
    }

    #region AwardToUser Tests

    [TestMethod]
    public async Task AwardToUser_AchievementDoesNotExist_ReturnsFalse()
    {
        _achievementRepository.GetAll().Returns(new List<Achievement>());
        var result = await _sut.AwardToUser(_testUser.Id, Guid.NewGuid());
        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task AwardToUser_UserAlreadyHasAchievement_ReturnsFalse()
    {
        var achievementId = Guid.NewGuid();
        var achievement = new Achievement { Id = achievementId, Name = "A1" };
        var entry = new AchievementEntry { Achievement = achievement, User = _testUser };
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry> { entry });

        var result = await _sut.AwardToUser(_testUser.Id, achievementId);

        result.Should().BeFalse();
        await _achievementRepository.DidNotReceive().CreateEntryRange(Arg.Any<IEnumerable<AchievementEntry>>());
    }

    [TestMethod]
    public async Task AwardToUser_UserDoesNotHaveAchievement_AwardsAnnouncesAndReturnsTrue()
    {
        var achievementId = Guid.NewGuid();
        var achievement = new Achievement { Id = achievementId, Name = "A1" };
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry>());

        var result = await _sut.AwardToUser(_testUser.Id, achievementId);

        result.Should().BeTrue();
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(entries =>
            entries.Count() == 1 && entries.First().Achievement.Id == achievementId));
        await _purchaseNotifier.Received(1).NotifyAchievementAsync(Arg.Is<AchievementEvent>(e => e.AchievementName == "A1"));
        await _userNotifier.Received(1).NotifyAsync(_testUser.Id, "Nieuwe prestatie ontgrendeld!", Arg.Any<string>(), "/achievements", true);
    }

    [TestMethod]
    public async Task AwardToUser_WithMissingParentTier_AwardsParentFirst()
    {
        var parent = new Achievement { Id = Guid.NewGuid(), Name = "Bronze" };
        var child = new Achievement { Id = Guid.NewGuid(), Name = "Silver", ParentAchievementId = parent.Id };
        _achievementRepository.GetAll().Returns(new List<Achievement> { parent, child });
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry>());

        var result = await _sut.AwardToUser(_testUser.Id, child.Id);

        result.Should().BeTrue();
        Received.InOrder(() =>
        {
            _achievementRepository.CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(e => e.Single().Achievement.Id == parent.Id));
            _achievementRepository.CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(e => e.Single().Achievement.Id == child.Id));
        });
    }

    #endregion

    #region AwardToAllUsers Tests

    [TestMethod]
    public async Task AwardToAllUsers_AchievementDoesNotExist_ReturnsZero()
    {
        _applicationUserRepository.GetAll().Returns(new List<ApplicationUser>());
        _achievementRepository.GetAll().Returns(new List<Achievement>());
        var result = await _sut.AwardToAllUsers(Guid.NewGuid());
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task AwardToAllUsers_NoUsers_ReturnsZero()
    {
        var achievement = new Achievement { Id = Guid.NewGuid(), Name = "A1" };
        _applicationUserRepository.GetAll().Returns(new List<ApplicationUser>());
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        var result = await _sut.AwardToAllUsers(achievement.Id);
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task AwardToAllUsers_AllUsersAlreadyHaveAchievement_ReturnsZero()
    {
        var achievement = new Achievement { Id = Guid.NewGuid(), Name = "A1" };
        var entry = new AchievementEntry { Achievement = achievement, User = _testUser };
        _applicationUserRepository.GetAll().Returns(new List<ApplicationUser> { _testUser });
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        _achievementRepository.GetAllEntriesOfUser(_testUser.Id).Returns(new List<AchievementEntry> { entry });
        var result = await _sut.AwardToAllUsers(achievement.Id);
        result.Should().Be(0);
    }

    [TestMethod]
    public async Task AwardToAllUsers_SomeUsersDoNotHaveAchievement_AwardsAndReturnsCount()
    {
        var achievement = new Achievement { Id = Guid.NewGuid(), Name = "A1" };
        var user1 = new ApplicationUser { Id = "u1" };
        var user2 = new ApplicationUser { Id = "u2" };
        _applicationUserRepository.GetAll().Returns(new List<ApplicationUser> { user1, user2 });
        _achievementRepository.GetAll().Returns(new List<Achievement> { achievement });
        _achievementRepository.GetAllEntriesOfUser("u1").Returns(new List<AchievementEntry>());
        _achievementRepository.GetAllEntriesOfUser("u2").Returns(new List<AchievementEntry> { new AchievementEntry { Achievement = achievement, User = user2 } });

        var result = await _sut.AwardToAllUsers(achievement.Id);

        result.Should().Be(1);
        await _achievementRepository.Received(1).CreateEntryRange(Arg.Is<IEnumerable<AchievementEntry>>(entries => entries.Count() == 1 && entries.First().User.Id == "u1"));
    }

    #endregion
}
