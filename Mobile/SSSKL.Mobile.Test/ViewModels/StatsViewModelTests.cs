using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class StatsViewModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IAppNavigator _navigator = Substitute.For<IAppNavigator>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();

    private StatsViewModel CreateSut(params string[] roles)
    {
        _api.GetMeAsync(Arg.Any<CancellationToken>()).Returns(new CurrentUser { Id = "me", FullName = "Ik", Roles = roles.Length == 0 ? [RoleNames.User] : roles });
        _api.GetAchievementEntriesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        return new StatsViewModel(_api, new CurrentUserService(_api), _navigator, _dialogs,
            new AppEndpoints(new Uri("https://api.test"), new Uri("https://web.test")), new FakeTimeProvider(Now),
            NullLogger<StatsViewModel>.Instance);
    }

    private static UserStats Stats(DateTime? recalculatedAt = null) => new()
    {
        UserId = "me", TotalOrders = 1234, TotalSpent = 56.5m, CurrentStreak = 3,
        MinMinutesBetweenOrders = UserStats.NoMinutes, MinMinutesBetweenTopUp = 15,
        LastOrderDate = new DateTime(2026, 11, 10, 20, 15, 0), LastTopUpDate = new DateTime(1, 1, 1),
        LastStatsRecalculatedAt = recalculatedAt
    };

    [TestMethod]
    public async Task Load_Own_ShowsHighlightsAndSections()
    {
        // Arrange
        _api.GetStatsAsync("me", Arg.Any<CancellationToken>()).Returns(Stats());
        var sut = CreateSut();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Title.Should().Be(Texts.MyStats);
        sut.Highlights.Select(h => h.Value).Should().Equal("1.234", "€ 56,50", "3 dagen", "0");
        var lines = sut.Sections.SelectMany(s => s.Lines).ToDictionary(l => l.Label, l => l.Value);
        lines[Texts.StatMinBetweenOrders].Should().Be(Texts.NoData);
        lines[Texts.StatMinBetweenTopUps].Should().Be("15 min.");
        lines[Texts.StatLastOrder].Should().Be("10 nov 2026, 21:15");
        lines[Texts.StatLastTopUp].Should().Be(Texts.NotHappenedYet);
        sut.HasStats.Should().BeTrue();
        sut.CanRecalculate.Should().BeTrue("never recalculated");
    }

    [TestMethod]
    public async Task Load_UserAsksForSomeoneElse_GetsTheirOwn()
    {
        // Arrange
        _api.GetStatsAsync("me", Arg.Any<CancellationToken>()).Returns(Stats());
        var sut = CreateSut();
        sut.Initialize("other");

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        await _api.DidNotReceive().GetStatsAsync("other", Arg.Any<CancellationToken>());
        sut.Title.Should().Be(Texts.MyStats);
    }

    [TestMethod]
    public async Task Load_AdminAsksForSomeoneElse_ShowsTheirs()
    {
        // Arrange
        _api.GetStatsAsync("other", Arg.Any<CancellationToken>()).Returns(Stats());
        _api.GetUserProfileAsync("other", Arg.Any<CancellationToken>()).Returns(new UserSummary { Id = "other", FullName = "Anna" });
        var sut = CreateSut(RoleNames.Admin);
        sut.Initialize("other");

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Title.Should().Be(Texts.StatsOf("Anna"));
        sut.Subtitle.Should().Be(Texts.StatsSubtitle);
    }

    [TestMethod]
    [DataRow(6, false)]
    [DataRow(7, true)]
    public async Task CanRecalculate_OnceEverySevenDaysForUsers(int daysAgo, bool expected)
    {
        // Arrange
        _api.GetStatsAsync("me", Arg.Any<CancellationToken>()).Returns(Stats(Now.UtcDateTime.AddDays(-daysAgo)));
        var sut = CreateSut();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.CanRecalculate.Should().Be(expected);
        sut.RecalculateCommand.CanExecute(null).Should().Be(expected);
        sut.LastRecalculatedText.Should().NotBeNull();
    }

    [TestMethod]
    public async Task CanRecalculate_AdminsAlways()
    {
        // Arrange
        _api.GetStatsAsync("me", Arg.Any<CancellationToken>()).Returns(Stats(Now.UtcDateTime.AddHours(-1)));
        var sut = CreateSut(RoleNames.Admin);

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.CanRecalculate.Should().BeTrue();
    }

    [TestMethod]
    public async Task Recalculate_TooSoon_SaysSoAndDisables()
    {
        // Arrange
        _api.GetStatsAsync("me", Arg.Any<CancellationToken>()).Returns(Stats());
        _api.RecalculateStatsAsync("me", Arg.Any<CancellationToken>()).ThrowsAsync(new ApiException(HttpStatusCode.TooManyRequests, "Too Many Requests"));
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.RecalculateCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.StatsRecalculateTooSoon);
        sut.CanRecalculate.Should().BeFalse();
    }

    [TestMethod]
    public async Task Recalculate_ShowsTheNewFigures()
    {
        // Arrange
        _api.GetStatsAsync("me", Arg.Any<CancellationToken>()).Returns(Stats());
        _api.RecalculateStatsAsync("me", Arg.Any<CancellationToken>()).Returns(Stats(Now.UtcDateTime) with { TotalOrders = 5 });
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.RecalculateCommand.ExecuteAsync(null);

        // Assert
        sut.Highlights[0].Value.Should().Be("5");
        sut.CanRecalculate.Should().BeFalse("just recalculated");
        await _dialogs.Received(1).ToastAsync(Texts.StatsRecalculated);
    }
}
