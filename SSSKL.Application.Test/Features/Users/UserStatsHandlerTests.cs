using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SSSKL.Application.Features.Users.Queries.GetPersonalData;
using SSSKL.Application.Features.Users.Stats;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Test.Features.Users;

[TestClass]
public class UserStatsHandlerTests
{
    private IApplicationUserRepository _userRepository = null!;
    private IUserStatRepository _userStatRepository = null!;

    [TestInitialize]
    public void Init()
    {
        _userRepository = Substitute.For<IApplicationUserRepository>();
        _userStatRepository = Substitute.For<IUserStatRepository>();
        _userRepository.FindById("user1").Returns(new ApplicationUser { Id = "user1", UserName = "owner" });
    }

    [TestMethod]
    public async Task GetStats_AsOwner_ReturnsStats()
    {
        _userStatRepository.GetOrCreateByUserId("user1").Returns(new UserStat { UserId = "user1", TotalOrders = 2, TotalSpent = 3.5m });
        var sut = new GetUserStatsHandler(_userRepository, _userStatRepository);

        var result = await sut.Handle(new GetUserStatsQuery("user1", "user1", IsAdmin: false), CancellationToken.None);

        result.TotalOrders.Should().Be(2);
        result.TotalSpent.Should().Be(3.5m);
    }

    [TestMethod]
    public async Task GetStats_WhenNotOwnerAndNotAdmin_ThrowsUnauthorized()
    {
        var sut = new GetUserStatsHandler(_userRepository, _userStatRepository);

        var act = () => sut.Handle(new GetUserStatsQuery("user1", "other-user", IsAdmin: false), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _userStatRepository.DidNotReceive().GetOrCreateByUserId(Arg.Any<string>());
    }

    [TestMethod]
    public async Task GetStats_WhenUserMissing_ThrowsNotFound()
    {
        var sut = new GetUserStatsHandler(_userRepository, _userStatRepository);

        var act = () => sut.Handle(new GetUserStatsQuery("missing", "missing", IsAdmin: true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Recalculate_WhenNotAdminAndTargetIsAnotherUser_ThrowsUnauthorized()
    {
        var sut = new RecalculateUserStatsHandler(_userRepository, _userStatRepository);

        var act = () => sut.Handle(new RecalculateUserStatsCommand("user1", "other-user", IsAdmin: false), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _userStatRepository.DidNotReceive().RecalculateByUserId(Arg.Any<string>());
    }

    [TestMethod]
    public async Task Recalculate_AsAdmin_SkipsRateLimitAndRecalculates()
    {
        _userStatRepository.RecalculateByUserId("user1").Returns(new UserStat { UserId = "user1", TotalOrders = 9 });
        var sut = new RecalculateUserStatsHandler(_userRepository, _userStatRepository);

        var result = await sut.Handle(new RecalculateUserStatsCommand("user1", "admin", IsAdmin: true), CancellationToken.None);

        result.Stats!.TotalOrders.Should().Be(9);
        await _userStatRepository.DidNotReceive().TryMarkRecalculatedAtIfEligible(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<TimeSpan>());
    }

    [TestMethod]
    public async Task Recalculate_AsOwnerWithinSevenDays_ReturnsNextAllowedAt()
    {
        var last = DateTime.UtcNow.AddDays(-2);
        _userStatRepository.TryMarkRecalculatedAtIfEligible("user1", Arg.Any<DateTime>(), TimeSpan.FromDays(7)).Returns(false);
        _userStatRepository.GetOrCreateByUserId("user1").Returns(new UserStat { UserId = "user1", LastStatsRecalculatedAt = last });
        var sut = new RecalculateUserStatsHandler(_userRepository, _userStatRepository);

        var result = await sut.Handle(new RecalculateUserStatsCommand("user1", "user1", IsAdmin: false), CancellationToken.None);

        result.Stats.Should().BeNull();
        result.NextAllowedAt.Should().Be(last.AddDays(7));
        await _userStatRepository.DidNotReceive().RecalculateByUserId(Arg.Any<string>());
    }

    [TestMethod]
    public async Task Recalculate_AsOwnerWhenEligible_Recalculates()
    {
        _userStatRepository.TryMarkRecalculatedAtIfEligible("user1", Arg.Any<DateTime>(), TimeSpan.FromDays(7)).Returns(true);
        _userStatRepository.RecalculateByUserId("user1").Returns(new UserStat { UserId = "user1" });
        var sut = new RecalculateUserStatsHandler(_userRepository, _userStatRepository);

        var result = await sut.Handle(new RecalculateUserStatsCommand("user1", "user1", IsAdmin: false), CancellationToken.None);

        result.Stats.Should().NotBeNull();
    }

    [TestMethod]
    public async Task PersonalData_ContainsPersonalDataPropertiesOnly()
    {
        var user = new ApplicationUser { Id = "u-personal", UserName = "personal.user", Name = "Personal", Surname = "User", Saldo = 12.34m };
        var userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        userManager.FindByIdAsync(user.Id).Returns(user);
        var sut = new GetPersonalDataHandler(userManager, NullLogger<GetPersonalDataHandler>.Instance);

        var result = await sut.Handle(new GetPersonalDataQuery(user.Id), CancellationToken.None);

        result.Should().ContainKey(nameof(ApplicationUser.Name)).WhoseValue.Should().Be("Personal");
        result.Should().ContainKey(nameof(ApplicationUser.Surname));
        result.Should().NotContainKey("Authenticator Key");
        _ = userManager.DidNotReceive().GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>());
    }

    [TestMethod]
    public async Task PersonalData_WhenUserMissing_ThrowsNotFound()
    {
        var userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        var sut = new GetPersonalDataHandler(userManager, NullLogger<GetPersonalDataHandler>.Instance);

        var act = () => sut.Handle(new GetPersonalDataQuery("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
