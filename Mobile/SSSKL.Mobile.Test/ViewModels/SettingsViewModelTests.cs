using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class SettingsViewModelTests
{
    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private readonly IAppNavigator _navigator = Substitute.For<IAppNavigator>();
    private readonly ISettingsStore _store = Substitute.For<ISettingsStore>();
    private readonly IThemeApplier _applier = Substitute.For<IThemeApplier>();

    private static readonly CurrentUser Jan = new()
    {
        Id = "u1", UserName = "jan", FullName = "Jan Jansen", Name = "Jan", Surname = "Jansen",
        PhoneNumber = "0612345678", Email = "jan@example.com", EmailConfirmed = true,
        Roles = [RoleNames.User, RoleNames.Kiosk]
    };

    [TestInitialize]
    public void Setup()
    {
        _api.GetMeAsync(Arg.Any<CancellationToken>()).Returns(Jan);
        _api.GetTwoFactorStatusAsync(Arg.Any<CancellationToken>()).Returns(new TwoFactorStatus { IsTwoFactorEnabled = true });
    }

    private SettingsViewModel CreateSut() => new(
        _api, new CurrentUserService(_api), _dialogs, _navigator, new ThemePreference(_store, _applier),
        NullLogger<SettingsViewModel>.Instance);

    [TestMethod]
    public async Task LoadAsync_ShowsProfileAndAccountStatus()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Name.Should().Be("Jan");
        sut.Surname.Should().Be("Jansen");
        sut.PhoneNumber.Should().Be("0612345678");
        sut.Email.Should().Be("jan@example.com");
        sut.EmailStatus.Should().Be(Texts.EmailConfirmed);
        sut.TwoFactorStatus.Should().Be(Texts.On);
        sut.RolesText.Should().Be("User, Kiosk");
        sut.IsDirty.Should().BeFalse();
        sut.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [TestMethod]
    public async Task LoadAsync_TwoFactorFails_LeavesItsStatusEmpty()
    {
        // Arrange
        _api.GetTwoFactorStatusAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new ApiException(System.Net.HttpStatusCode.InternalServerError, "x"));
        var sut = CreateSut();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.TwoFactorStatus.Should().BeEmpty();
        sut.Name.Should().Be("Jan");
        await _dialogs.DidNotReceiveWithAnyArgs().AlertAsync(default!, default!);
    }

    [TestMethod]
    public async Task LoadAsync_UserFails_ShowsAnError()
    {
        // Arrange
        _api.GetMeAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.LoadFailed);
    }

    [TestMethod]
    public async Task LoadAsync_WhileEditing_KeepsTheEdits()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);
        sut.Name = "Johannes";

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.Name.Should().Be("Johannes");
        sut.IsDirty.Should().BeTrue();
    }

    [TestMethod]
    public async Task SaveCommand_SendsTrimmedFieldsAndEmptyAsUnchanged()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);
        sut.Name = "  Johannes ";
        sut.PhoneNumber = "";

        // Act
        sut.SaveCommand.CanExecute(null).Should().BeTrue();
        await sut.SaveCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).UpdateMeAsync(new SelfUpdate("Johannes", "Jansen", null), Arg.Any<CancellationToken>());
        await _dialogs.Received(1).ToastAsync(Texts.SettingsSaved);
        sut.IsSaving.Should().BeFalse();
        // Shows what the server stored afterwards (it kept the phone number).
        sut.PhoneNumber.Should().Be("0612345678");
        sut.IsDirty.Should().BeFalse();
    }

    [TestMethod]
    public async Task SaveCommand_Fails_ShowsAnErrorAndKeepsTheEdits()
    {
        // Arrange
        _api.UpdateMeAsync(Arg.Any<SelfUpdate>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = CreateSut();
        await sut.LoadCommand.ExecuteAsync(null);
        sut.Surname = "de Vries";

        // Act
        await sut.SaveCommand.ExecuteAsync(null);

        // Assert
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.SettingsSaveFailed);
        sut.Surname.Should().Be("de Vries");
        sut.IsDirty.Should().BeTrue();
    }

    [TestMethod]
    public void SetThemeCommand_StoresAndApplies()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetThemeCommand.Execute(ThemeMode.Dark);

        // Assert
        sut.IsDark.Should().BeTrue();
        sut.IsAuto.Should().BeFalse();
        sut.IsLight.Should().BeFalse();
        _store.Received(1).Set(ThemePreference.StoreKey, "dark");
        _applier.Received(1).Apply(ThemeMode.Dark);
    }

    [TestMethod]
    public void Theme_StartsFromTheStoredMode()
    {
        // Arrange
        _store.Get(ThemePreference.StoreKey).Returns("light");

        // Act & Assert
        CreateSut().IsLight.Should().BeTrue();
    }

    [TestMethod]
    public async Task OpenCommand_OpensTheSettingsTab()
    {
        // Act
        await CreateSut().OpenCommand.ExecuteAsync(SettingsViewModel.SecurityPath);

        // Assert
        await _navigator.Received(1).OpenAsync("settings?tab=security");
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
