using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

/// <summary>Achievements list, achievement detail and member profiles.</summary>
[TestClass]
public class AchievementPagesViewModelTests
{
    private static readonly Guid Beer = Guid.NewGuid();
    private static readonly Guid Parent = Guid.NewGuid();

    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IAppNavigator _navigator = Substitute.For<IAppNavigator>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private readonly AppEndpoints _endpoints = new(new Uri("https://api.test"), new Uri("https://web.test"));
    private CurrentUserService _currentUser = null!;

    private void SignIn(params string[] roles)
    {
        _api.GetMeAsync(Arg.Any<CancellationToken>()).Returns(new CurrentUser { Id = "me", FullName = "Ik", Roles = roles.Length == 0 ? [RoleNames.User] : roles });
        _currentUser = new CurrentUserService(_api);
    }

    private static AchievementListing Listing(string name, bool completed, DateTime? date = null, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(), Name = name, Description = "…", Completed = completed, DateAdded = date, Tier = "Bronze"
    };

    [TestMethod]
    public async Task Achievements_EarnedFirstNewestFirstThenByName()
    {
        // Arrange
        SignIn();
        _api.GetAchievementListingAsync("me", Arg.Any<CancellationToken>()).Returns(
        [
            Listing("Zebra", false),
            Listing("Oud", true, new DateTime(2025, 1, 1)),
            Listing("Appel", false),
            Listing("Nieuw", true, new DateTime(2026, 5, 1)),
        ]);
        var sut = new AchievementsViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<AchievementsViewModel>.Instance);

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Items.Select(i => i.Name).Should().Equal("Nieuw", "Oud", "Appel", "Zebra");
        sut.Items[0].StatusText.Should().Be(Texts.AchievedOn("1 mei 2026"));
        sut.Items[2].StatusText.Should().Be(Texts.NotAchievedYet);
        sut.Subtitle.Should().Be(Texts.EarnedCount(2, 4));
        sut.Title.Should().Be(Texts.Achievements);

        sut.SetFilterCommand.Execute(AchievementFilter.Locked);
        sut.Items.Select(i => i.Name).Should().Equal("Appel", "Zebra");
        sut.SetFilterCommand.Execute(AchievementFilter.Earned);
        sut.Items.Should().OnlyContain(i => i.Completed);
    }

    [TestMethod]
    public async Task Achievements_OfSomeoneElse_ShowsTheirName()
    {
        // Arrange
        SignIn();
        _api.GetAchievementListingAsync("anna", Arg.Any<CancellationToken>()).Returns([Listing("Bier", true, DateTime.UtcNow)]);
        _api.GetUserProfileAsync("anna", Arg.Any<CancellationToken>()).Returns(new UserSummary { Id = "anna", FullName = "Anna" });
        var sut = new AchievementsViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<AchievementsViewModel>.Instance);
        sut.Initialize("anna");

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Title.Should().Be(Texts.AchievementsOf("Anna"));
    }

    private AchievementDetailViewModel Detail()
    {
        var sut = new AchievementDetailViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<AchievementDetailViewModel>.Instance);
        sut.Initialize(Beer);
        return sut;
    }

    private static AchievementEntry Earner(string userId, string fullName, DateTime date) => new()
    {
        Id = Guid.NewGuid(), AchievementId = Beer, AchievementName = "Bier", AchievementDescription = "Eerste bier", Tier = "Gold",
        DateAdded = date, UserId = userId, UserName = userId, UserFullName = fullName
    };

    [TestMethod]
    public async Task Detail_UsesTheListingAndOrdersEarnersByDate()
    {
        // Arrange
        SignIn();
        _api.GetAchievementEarnersAsync(Beer, Arg.Any<CancellationToken>()).Returns(
        [
            Earner("me", "Ik", new DateTime(2026, 3, 1)),
            Earner("anna", "Anna", new DateTime(2026, 1, 1)),
        ]);
        _api.GetAchievementListingAsync("me", Arg.Any<CancellationToken>()).Returns(
        [
            Listing("Bier", true, new DateTime(2026, 3, 1), Beer) with { Tier = "Gold", ImageUrl = "/img/bier.png", ParentAchievementId = Parent, ParentAchievementName = "Drinker" }
        ]);
        var sut = Detail();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Name.Should().Be("Bier");
        sut.Tier.Should().Be("Gold");
        sut.ImageUrl.Should().Be("https://api.test/img/bier.png");
        sut.Completed.Should().BeTrue();
        sut.ParentText.Should().Be(Texts.PartOf("Drinker"));
        sut.Earners.Select(e => e.Name).Should().Equal("Anna", "Ik");
        sut.Earners[1].IsSelf.Should().BeTrue();
        sut.Earners[0].Subtitle.Should().Be("@anna");
        sut.EarnedByText.Should().Be(Texts.EarnedByCount(2));
        sut.IsFound.Should().BeTrue();

        await sut.OpenParentCommand.ExecuteAsync(null);
        await _navigator.Received(1).OpenAsync($"achievements/{Parent}");
    }

    [TestMethod]
    public async Task Detail_NotInTheListing_FallsBackToTheEarners()
    {
        // Arrange
        SignIn();
        _api.GetAchievementEarnersAsync(Beer, Arg.Any<CancellationToken>()).Returns([Earner("anna", "Anna", DateTime.UtcNow)]);
        _api.GetAchievementListingAsync("me", Arg.Any<CancellationToken>()).Returns([]);
        var sut = Detail();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Name.Should().Be("Bier");
        sut.Completed.Should().BeFalse();
        sut.IsNotFound.Should().BeFalse();
    }

    [TestMethod]
    public async Task Detail_Unknown_IsNotFound()
    {
        // Arrange
        SignIn();
        _api.GetAchievementEarnersAsync(Beer, Arg.Any<CancellationToken>()).ThrowsAsync(new ApiException(HttpStatusCode.NotFound, "Not Found"));
        _api.GetAchievementListingAsync("me", Arg.Any<CancellationToken>()).Returns([]);
        var sut = Detail();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.IsNotFound.Should().BeTrue();
        sut.IsFound.Should().BeFalse();
        await _dialogs.DidNotReceiveWithAnyArgs().AlertAsync(default!, default!);
    }

    private UserProfileViewModel Profile()
    {
        _api.GetUserProfileAsync("anna", Arg.Any<CancellationToken>()).Returns(new UserSummary
        {
            Id = "anna", UserName = "anna", FullName = "Anna de Vries", Description = "<p>Hoi</p>"
        });
        _api.GetAchievementEntriesAsync("anna", Arg.Any<CancellationToken>()).Returns(
            Enumerable.Range(1, 6).Select(i => Earner("anna", "Anna", new DateTime(2026, i, 1)) with { AchievementName = $"A{i}" }).ToList());
        _api.GetStatsAsync("anna", Arg.Any<CancellationToken>()).Returns(new UserStats { TotalOrders = 42, CurrentStreak = 1 });
        var sut = new UserProfileViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<UserProfileViewModel>.Instance);
        sut.Initialize("anna");
        return sut;
    }

    [TestMethod]
    public async Task Profile_User_SeesProfileAndLatestAchievementsButNoStats()
    {
        // Arrange
        SignIn();
        var sut = Profile();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.DisplayName.Should().Be("Anna de Vries");
        sut.UserName.Should().Be("@anna");
        sut.Initials.Should().Be("AV");
        sut.DescriptionHtml.Should().Be("<p>Hoi</p>");
        sut.RecentAchievements.Select(a => a.Name).Should().Equal("A6", "A5", "A4", "A3");
        sut.HasStats.Should().BeFalse();
        await _api.DidNotReceiveWithAnyArgs().GetStatsAsync(default!, default);
    }

    [TestMethod]
    public async Task Profile_Admin_AlsoSeesStats()
    {
        // Arrange
        SignIn(RoleNames.Admin);
        var sut = Profile();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.HasStats.Should().BeTrue();
        sut.TotalOrders.Should().Be("42");
        sut.CurrentStreak.Should().Be("1 dag");

        await sut.OpenStatsCommand.ExecuteAsync(null);
        await _navigator.Received(1).OpenAsync("stats?userId=anna");
    }
}
