using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

/// <summary>Saldo list and leaderboard.</summary>
[TestClass]
public class MemberListViewModelTests
{
    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IAppNavigator _navigator = Substitute.For<IAppNavigator>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private readonly AppEndpoints _endpoints = new(new Uri("https://api.test"), new Uri("https://web.test"));
    private CurrentUserService _currentUser = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _currentUser = new CurrentUserService(_api);
        _api.GetMeAsync(Arg.Any<CancellationToken>()).Returns(new CurrentUser { Id = "me", FullName = "Ik Zelf" });
        await _currentUser.RefreshAsync();
    }

    private SaldoListViewModel SaldoList() =>
        new(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<SaldoListViewModel>.Instance);

    private LeaderboardViewModel Leaderboard() =>
        new(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<LeaderboardViewModel>.Instance);

    [TestMethod]
    public async Task SaldoList_SortsLowestFirstAndMarksYou()
    {
        // Arrange
        _api.GetUsersAsync(0, SaldoListViewModel.MaxUsers, Arg.Any<CancellationToken>()).Returns(new Page<UserSummary>
        {
            Items =
            [
                new UserSummary { Id = "a", FullName = "Anna", Saldo = 10m },
                new UserSummary { Id = "me", FullName = "Ik Zelf", Saldo = -5m, ProfilePictureUrl = "/api/v1/blob/me.png" },
                new UserSummary { Id = "b", FullName = "Bert", Saldo = 0m },
            ],
            TotalCount = 3
        });
        var sut = SaldoList();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Members.Select(m => m.Name).Should().Equal("Ik Zelf", "Bert", "Anna");
        sut.Members[0].Should().Match<MemberRow>(m =>
            m.IsSelf && m.IsDetailNegative && m.Detail == "€ -5,00" && m.PictureUrl == "https://api.test/api/v1/blob/me.png" && m.Initials == "IZ");
    }

    [TestMethod]
    public async Task SaldoList_Query_FiltersOnName()
    {
        // Arrange
        _api.GetUsersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new Page<UserSummary>
        {
            Items = [new UserSummary { Id = "a", FullName = "Anna" }, new UserSummary { Id = "b", FullName = "Bert" }],
            TotalCount = 2
        });
        var sut = SaldoList();
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        sut.Query = "ann";

        // Assert
        sut.Members.Select(m => m.Name).Should().Equal("Anna");
        sut.Query = "zzz";
        sut.IsEmpty.Should().BeTrue();
    }

    [TestMethod]
    public async Task SaldoList_Open_OpensTheProfile()
    {
        // Act
        await SaldoList().OpenCommand.ExecuteAsync(new MemberRow("u 1", "Anna", null, "€ 0,00"));

        // Assert
        await _navigator.Received(1).OpenAsync("users/u%201");
    }

    [TestMethod]
    public async Task Leaderboard_PicksTheFirstLeaderboardProductAndRanks()
    {
        // Arrange
        var beer = new Product { Id = Guid.NewGuid(), Name = "Bier", EnableLeaderboard = true };
        _api.GetProductsAsync(Arg.Any<CancellationToken>()).Returns([new Product { Id = Guid.NewGuid(), Name = "Chips" }, beer]);
        _api.GetLeaderboardAsync(beer.Id, LeaderboardPeriod.Last12Hours, Arg.Any<CancellationToken>()).Returns(
        [
            new LeaderboardEntry { Position = 2, FullName = "Ik Zelf", Amount = 3, UserId = "me" },
            new LeaderboardEntry { Position = 1, FullName = "Anna", Amount = 1234, UserId = "a" },
        ]);
        var sut = Leaderboard();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Products.Should().Equal(beer);
        sut.SelectedProduct.Should().Be(beer);
        sut.Entries.Select(e => (e.Rank, e.Name, e.Detail)).Should().Equal((1, "Anna", "1.234"), (2, "Ik Zelf", "3"));
        sut.Entries[0].IsGold.Should().BeTrue();
        sut.Entries[1].IsSelf.Should().BeTrue();
    }

    [TestMethod]
    public async Task Leaderboard_SetPeriod_LoadsThatPeriod()
    {
        // Arrange
        var beer = new Product { Id = Guid.NewGuid(), Name = "Bier", EnableLeaderboard = true };
        _api.GetProductsAsync(Arg.Any<CancellationToken>()).Returns([beer]);
        _api.GetLeaderboardAsync(beer.Id, Arg.Any<LeaderboardPeriod>(), Arg.Any<CancellationToken>()).Returns([]);
        var sut = Leaderboard();
        await sut.LoadCommand.ExecuteAsync(null);
        sut.IsEmpty.Should().BeTrue();

        // Act
        await sut.SetPeriodCommand.ExecuteAsync(LeaderboardPeriod.Month);

        // Assert
        sut.IsMonth.Should().BeTrue();
        await _api.Received(1).GetLeaderboardAsync(beer.Id, LeaderboardPeriod.Month, Arg.Any<CancellationToken>());
    }
}
