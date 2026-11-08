using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Popups;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Test.Util;

namespace SSSKL.Mobile.Test.Popups;

[TestClass]
public class WhatsNewCoordinatorTests
{
    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IPopupPresenter _presenter = Substitute.For<IPopupPresenter>();
    private readonly InMemorySettingsStore _settings = new();

    private WhatsNewCoordinator CreateSut() => new(_api, _presenter, _settings, NullLogger<WhatsNewCoordinator>.Instance);

    [TestMethod]
    public async Task CheckAsync_NewVersion_ShowsAndStoresVersion()
    {
        // Arrange
        _api.GetGlobalSettingAsync(WhatsNewCoordinator.SettingKey).Returns(new GlobalSetting("WhatsNewContent", "<p>Nieuw</p>", "2026-11-01T10:00:00Z"));

        // Act
        await CreateSut().CheckAsync();

        // Assert
        await _presenter.Received(1).ShowWhatsNewAsync("<p>Nieuw</p>");
        _settings.Values[WhatsNewCoordinator.SeenVersionKey].Should().Be("2026-11-01T10:00:00Z");
    }

    [TestMethod]
    public async Task CheckAsync_VersionAlreadySeen_DoesNotShow()
    {
        // Arrange
        _settings.Set(WhatsNewCoordinator.SeenVersionKey, "v1");
        _api.GetGlobalSettingAsync(WhatsNewCoordinator.SettingKey).Returns(new GlobalSetting("WhatsNewContent", "<p>Nieuw</p>", "v1"));

        // Act
        await CreateSut().CheckAsync();

        // Assert
        await _presenter.DidNotReceiveWithAnyArgs().ShowWhatsNewAsync(default!);
    }

    [TestMethod]
    public async Task CheckAsync_EmptyOrMissingSetting_DoesNotShow()
    {
        // Arrange
        _api.GetGlobalSettingAsync(WhatsNewCoordinator.SettingKey).Returns(new GlobalSetting("WhatsNewContent", "  ", "v2"), (GlobalSetting?)null);
        var sut = CreateSut();

        // Act
        await sut.CheckAsync();
        await sut.CheckAsync();

        // Assert
        await _presenter.DidNotReceiveWithAnyArgs().ShowWhatsNewAsync(default!);
        _settings.Values.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CheckAsync_ApiFails_DoesNotThrow()
    {
        // Arrange
        _api.GetGlobalSettingAsync(default!).ThrowsAsyncForAnyArgs(new HttpRequestException("offline"));

        // Act
        var act = () => CreateSut().CheckAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}
