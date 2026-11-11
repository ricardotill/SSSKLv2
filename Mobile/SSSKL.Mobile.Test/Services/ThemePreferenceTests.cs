using FluentAssertions;
using NSubstitute;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Test.Services;

[TestClass]
public class ThemePreferenceTests
{
    private readonly ISettingsStore _store = Substitute.For<ISettingsStore>();
    private readonly IThemeApplier _applier = Substitute.For<IThemeApplier>();

    [TestMethod]
    [DataRow(null, ThemeMode.Auto)]
    [DataRow("auto", ThemeMode.Auto)]
    [DataRow("light", ThemeMode.Light)]
    [DataRow("dark", ThemeMode.Dark)]
    [DataRow("purple", ThemeMode.Auto)]
    public void Mode_ReadsTheStoredValue(string? stored, ThemeMode expected)
    {
        // Arrange
        _store.Get(ThemePreference.StoreKey).Returns(stored);

        // Act & Assert
        new ThemePreference(_store, _applier).Mode.Should().Be(expected);
    }

    [TestMethod]
    public void Set_StoresAppliesAndNotifies()
    {
        // Arrange
        var sut = new ThemePreference(_store, _applier);
        ThemeMode? raised = null;
        sut.Changed += (_, mode) => raised = mode;

        // Act
        sut.Set(ThemeMode.Dark);

        // Assert
        _store.Received(1).Set(ThemePreference.StoreKey, "dark");
        _applier.Received(1).Apply(ThemeMode.Dark);
        raised.Should().Be(ThemeMode.Dark);
    }

    [TestMethod]
    public void Set_SameMode_DoesNothing()
    {
        // Arrange
        _store.Get(ThemePreference.StoreKey).Returns("light");
        var sut = new ThemePreference(_store, _applier);
        var raised = false;
        sut.Changed += (_, _) => raised = true;

        // Act
        sut.Set(ThemeMode.Light);

        // Assert
        _store.DidNotReceiveWithAnyArgs().Set(default!, default!);
        _applier.DidNotReceiveWithAnyArgs().Apply(default);
        raised.Should().BeFalse();
    }

    [TestMethod]
    public void ApplyStored_AppliesTheStoredMode()
    {
        // Arrange
        _store.Get(ThemePreference.StoreKey).Returns("dark");

        // Act
        new ThemePreference(_store, _applier).ApplyStored();

        // Assert
        _applier.Received(1).Apply(ThemeMode.Dark);
    }
}
