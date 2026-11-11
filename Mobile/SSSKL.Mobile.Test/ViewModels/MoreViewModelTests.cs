using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class MoreViewModelTests
{
    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IAppNavigator _navigator = Substitute.For<IAppNavigator>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private CurrentUserService _currentUser = null!;

    private static readonly CurrentUser Jan = new()
    {
        Id = "u1", UserName = "jan", FullName = "Jan Jansen", Saldo = -2.5m, Roles = [RoleNames.User],
        ProfilePictureUrl = "/api/v1/blob/jan.png"
    };

    [TestInitialize]
    public void Setup()
    {
        _currentUser = new CurrentUserService(_api);
        _api.GetMeAsync(Arg.Any<CancellationToken>()).Returns(Jan);
        _api.CanAccessQuotesAsync(Arg.Any<CancellationToken>()).Returns(true);
    }

    private MoreViewModel CreateSut() => new(
        _api, _currentUser, _navigator, _dialogs,
        new AppEndpoints(new Uri("https://api.test"), new Uri("https://web.test")),
        NullLogger<MoreViewModel>.Instance);

    [TestMethod]
    public async Task LoadAsync_ShowsUserAndMenu()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.DisplayName.Should().Be("Jan Jansen");
        sut.Initials.Should().Be("JJ");
        sut.PictureUrl.Should().Be("https://api.test/api/v1/blob/jan.png");
        sut.HasPicture.Should().BeTrue();
        sut.IsSaldoNegative.Should().BeTrue();
        sut.Saldo.Should().Contain("2,50");
        sut.Sections.SelectMany(s => s.Entries).Should().NotContain(e => e.Path == "notifications", "Notificaties has its own tab");
        sut.Sections.SelectMany(s => s.Entries).Should().Contain(e => e.Path == "quotes");
    }

    [TestMethod]
    public async Task LoadAsync_ProbesFail_StillShowsTheMenu()
    {
        // Arrange
        _api.CanAccessQuotesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert: like the SPA, quotes stay visible when the probe fails.
        sut.Sections.SelectMany(s => s.Entries).Should().Contain(e => e.Path == "quotes");
        await _dialogs.DidNotReceiveWithAnyArgs().AlertAsync(default!, default!);
    }

    [TestMethod]
    public async Task LoadAsync_UserFailsWithoutCache_ShowsAnError()
    {
        // Arrange
        _api.GetMeAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();
        sut.IsRefreshing = true;

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.LoadFailed);
        sut.Sections.Should().BeEmpty();
        sut.IsRefreshing.Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadAsync_UserFailsWithCache_KeepsTheCachedUser()
    {
        // Arrange
        await _currentUser.RefreshAsync();
        _api.GetMeAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.DisplayName.Should().Be("Jan Jansen");
        sut.Sections.Should().NotBeEmpty();
        await _dialogs.DidNotReceiveWithAnyArgs().AlertAsync(default!, default!);
    }

    [TestMethod]
    public async Task LoadAsync_Unchanged_KeepsTheSameSectionObjects()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);
        var first = sut.Sections[0];

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert: no flicker on revisits.
        sut.Sections[0].Should().BeSameAs(first);
    }

    [TestMethod]
    [DataRow("events")]
    [DataRow("settings")]
    public async Task OpenCommand_HandsTheRouteToTheNavigator(string path)
    {
        // Act
        await CreateSut().OpenCommand.ExecuteAsync(new MenuEntry("x", Icons.Calendar, path));

        // Assert: the navigator decides native vs. WebView (NativeRoutes).
        await _navigator.Received(1).OpenAsync(path);
    }

    [TestMethod]
    public async Task OpenProfileCommand_SwitchesToTheProfileTab()
    {
        // Act
        await CreateSut().OpenProfileCommand.ExecuteAsync(null);

        // Assert
        await _navigator.Received(1).OpenProfileAsync();
    }

    [TestMethod]
    [DataRow(true, 1)]
    [DataRow(false, 0)]
    public async Task LogoutCommand_SignsOutOnlyWhenConfirmed(bool confirmed, int signOuts)
    {
        // Arrange
        _dialogs.ConfirmAsync(Texts.Logout, Texts.ConfirmLogout, Texts.Logout, Texts.Cancel).Returns(confirmed);

        // Act
        await CreateSut().LogoutCommand.ExecuteAsync(null);

        // Assert
        await _navigator.Received(signOuts).SignOutAsync();
    }
}
