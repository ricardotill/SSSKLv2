using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Test.Services;

[TestClass]
public class NotificationCenterTests
{
    private readonly ISssklApi _api = Substitute.For<ISssklApi>();

    private NotificationCenter CreateSut() => new(_api, NullLogger<NotificationCenter>.Instance);

    [TestMethod]
    [DataRow(0, null)]
    [DataRow(3, "3")]
    [DataRow(12, "9+")]
    public async Task RefreshAsync_SetsTheCountAndBadge(int count, string? badge)
    {
        // Arrange
        _api.GetUnreadNotificationCountAsync(Arg.Any<CancellationToken>()).Returns(count);
        var sut = CreateSut();

        // Act
        await sut.RefreshAsync();

        // Assert
        sut.UnreadCount.Should().Be(count);
        sut.BadgeText.Should().Be(badge);
    }

    [TestMethod]
    public async Task RefreshAsync_Fails_KeepsTheLastCount()
    {
        // Arrange
        _api.GetUnreadNotificationCountAsync(Arg.Any<CancellationToken>()).Returns(2, 0);
        var sut = CreateSut();
        await sut.RefreshAsync();
        _api.GetUnreadNotificationCountAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));

        // Act
        var act = () => sut.RefreshAsync();

        // Assert
        await act.Should().NotThrowAsync();
        sut.UnreadCount.Should().Be(2);
    }

    [TestMethod]
    public async Task MarkedRead_LowersTheCountButNotBelowZero()
    {
        // Arrange
        _api.GetUnreadNotificationCountAsync(Arg.Any<CancellationToken>()).Returns(1);
        var sut = CreateSut();
        await sut.RefreshAsync();

        // Act
        sut.MarkedRead();
        sut.MarkedRead();

        // Assert
        sut.UnreadCount.Should().Be(0);
        sut.BadgeText.Should().BeNull();
    }

    [TestMethod]
    public async Task RefreshAsync_StartedBeforeALocalChange_DoesNotUndoIt()
    {
        // Arrange: the server still reports 5 while the user clears them all.
        var pending = new TaskCompletionSource<int>();
        _api.GetUnreadNotificationCountAsync(Arg.Any<CancellationToken>()).Returns(pending.Task);
        var sut = CreateSut();
        var refresh = sut.RefreshAsync();

        // Act
        sut.Clear();
        pending.SetResult(5);
        await refresh;

        // Assert
        sut.UnreadCount.Should().Be(0);
    }

    [TestMethod]
    public async Task UnreadCount_RaisesBadgeTextChanged()
    {
        // Arrange
        _api.GetUnreadNotificationCountAsync(Arg.Any<CancellationToken>()).Returns(4);
        var sut = CreateSut();
        var changed = new List<string?>();
        sut.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        // Act
        await sut.RefreshAsync();

        // Assert
        changed.Should().Contain(nameof(NotificationCenter.BadgeText));
    }
}
