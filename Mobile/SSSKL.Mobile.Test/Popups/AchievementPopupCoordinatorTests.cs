using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Popups;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Test.Popups;

[TestClass]
public class AchievementPopupCoordinatorTests
{
    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IPopupPresenter _presenter = Substitute.For<IPopupPresenter>();
    private readonly FakeTimeProvider _time = new();

    private AchievementPopupCoordinator CreateSut() => new(_api, _presenter, _time, NullLogger<AchievementPopupCoordinator>.Instance);

    private static IReadOnlyList<AchievementEntry> Entries(int count) =>
        Enumerable.Range(0, count).Select(i => new AchievementEntry { Id = Guid.NewGuid(), AchievementName = $"A{i}" }).ToList();

    [TestMethod]
    public async Task CheckAsync_UnseenEntries_ShowsThem()
    {
        // Arrange
        var entries = Entries(2);
        _api.GetUnseenAchievementsAsync().Returns(entries);

        // Act
        await CreateSut().CheckAsync();

        // Assert
        await _presenter.Received(1).ShowAchievementsAsync(entries);
    }

    [TestMethod]
    public async Task CheckAsync_NoEntries_ShowsNothing()
    {
        // Arrange
        _api.GetUnseenAchievementsAsync().Returns(Entries(0));

        // Act
        await CreateSut().CheckAsync();

        // Assert
        await _presenter.DidNotReceiveWithAnyArgs().ShowAchievementsAsync(default!);
    }

    [TestMethod]
    public async Task CheckAsync_WhileRunning_DoesNotFetchTwice()
    {
        // Arrange
        var gate = new TaskCompletionSource<IReadOnlyList<AchievementEntry>>();
        _api.GetUnseenAchievementsAsync().Returns(gate.Task);
        var sut = CreateSut();

        // Act
        var first = sut.CheckAsync();
        var second = sut.CheckAsync();
        gate.SetResult(Entries(1));
        await Task.WhenAll(first, second);

        // Assert
        await _api.Received(1).GetUnseenAchievementsAsync();
        await _presenter.Received(1).ShowAchievementsAsync(Arg.Any<IReadOnlyList<AchievementEntry>>());
    }

    [TestMethod]
    public async Task CheckOnResumeAsync_ThrottlesWithinInterval()
    {
        // Arrange
        _api.GetUnseenAchievementsAsync().Returns(Entries(0));
        var sut = CreateSut();

        // Act
        await sut.CheckAsync();
        await sut.CheckOnResumeAsync();
        _time.Advance(AchievementPopupCoordinator.ResumeInterval);
        await sut.CheckOnResumeAsync();

        // Assert
        await _api.Received(2).GetUnseenAchievementsAsync();
    }
}
