using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using SSSKL.Mobile.Core.Chooser;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.Chooser;

[TestClass]
public class FingerChooserTests
{
    private readonly FakeTimeProvider _time = new();

    private FingerChooser Create(ChooserMode mode = ChooserMode.Winners, int count = 1, int seed = 42)
    {
        var chooser = new FingerChooser(_time, new Random(seed));
        chooser.Configure(mode, count);
        return chooser;
    }

    private static void Down(FingerChooser chooser, params long[] ids)
    {
        foreach (var id in ids)
        {
            chooser.TouchDown(id, id * 10, id * 10);
        }
    }

    private void RunOut(FingerChooser chooser)
    {
        _time.Advance(FingerChooser.CountdownDuration);
        chooser.Tick();
    }

    [TestMethod]
    public void Winners_WaitsForOneMoreFingerThanWinners()
    {
        // Arrange
        var chooser = Create(ChooserMode.Winners, 2);

        // Act
        Down(chooser, 1, 2);

        // Assert
        chooser.Phase.Should().Be(ChooserPhase.Waiting);
        chooser.RequiredFingers.Should().Be(3);
        Down(chooser, 3);
        chooser.Phase.Should().Be(ChooserPhase.Counting);
    }

    [TestMethod]
    public void Countdown_RestartsWhenAFingerJoinsOrLeaves()
    {
        // Arrange
        var chooser = Create();
        Down(chooser, 1, 2);
        _time.Advance(TimeSpan.FromSeconds(2));

        // Act: a third finger joins just before the end
        Down(chooser, 3);
        _time.Advance(TimeSpan.FromSeconds(2));

        // Assert: not decided yet (only 2s since the change)
        chooser.Tick().Should().BeFalse();
        chooser.Progress.Should().BeApproximately(2.0 / 3, 0.001);

        chooser.TouchUp(3);
        chooser.Progress.Should().Be(0);
        _time.Advance(TimeSpan.FromSeconds(2.9));
        chooser.Tick().Should().BeFalse();
        _time.Advance(TimeSpan.FromSeconds(0.1));
        chooser.Tick().Should().BeTrue();
        chooser.Phase.Should().Be(ChooserPhase.Decided);
    }

    [TestMethod]
    public void LiftingBelowTheMinimum_StopsTheCountdown()
    {
        var chooser = Create();
        Down(chooser, 1, 2);

        chooser.TouchUp(2);

        chooser.Phase.Should().Be(ChooserPhase.Waiting);
        RunOut(chooser);
        chooser.Phase.Should().Be(ChooserPhase.Waiting);
    }

    [TestMethod]
    [DataRow(1, 2)]
    [DataRow(1, 6)]
    [DataRow(3, 7)]
    public void Winners_PicksExactlyCountWinners(int winners, int fingers)
    {
        // Arrange
        var chooser = Create(ChooserMode.Winners, winners);
        var decided = 0;
        chooser.Decided += (_, _) => decided++;
        Down(chooser, Enumerable.Range(1, fingers).Select(i => (long)i).ToArray());

        // Act
        RunOut(chooser);

        // Assert
        decided.Should().Be(1);
        chooser.Fingers.Count(f => f.IsWinner).Should().Be(winners);
        chooser.Fingers.Should().OnlyContain(f => f.Team == -1);
    }

    [TestMethod]
    public void Winners_EveryFingerCanWin()
    {
        var winners = new HashSet<long>();
        for (var seed = 0; seed < 200; seed++)
        {
            var chooser = Create(seed: seed);
            Down(chooser, 1, 2, 3, 4);
            RunOut(chooser);
            winners.Add(chooser.Fingers.Single(f => f.IsWinner).Id);
        }

        winners.Should().BeEquivalentTo([1L, 2L, 3L, 4L]);
    }

    [TestMethod]
    [DataRow(2, 2)]
    [DataRow(2, 5)]
    [DataRow(3, 7)]
    [DataRow(5, 10)]
    public void Teams_GivesEveryFingerATeam_Balanced(int teams, int fingers)
    {
        // Arrange
        var chooser = Create(ChooserMode.Teams, teams);
        Down(chooser, Enumerable.Range(1, fingers).Select(i => (long)i).ToArray());

        // Act
        RunOut(chooser);

        // Assert
        chooser.Fingers.Should().OnlyContain(f => f.Team >= 0 && f.Team < teams && !f.IsWinner);
        var sizes = chooser.Fingers.GroupBy(f => f.Team).Select(g => g.Count()).ToList();
        sizes.Should().HaveCount(Math.Min(teams, fingers));
        (sizes.Max() - sizes.Min()).Should().BeLessThanOrEqualTo(1);
    }

    [TestMethod]
    public void Decided_IgnoresNewFingers_AndResetsWhenAllAreLifted()
    {
        // Arrange
        var chooser = Create();
        Down(chooser, 1, 2, 3);
        RunOut(chooser);

        // Act / Assert
        chooser.TouchDown(4, 0, 0).Should().BeFalse();
        chooser.TouchUp(1);
        chooser.TouchUp(2);
        chooser.Phase.Should().Be(ChooserPhase.Decided);
        chooser.Fingers.Should().HaveCount(3, "lifted fingers keep showing the result");
        chooser.TouchUp(3);
        chooser.Phase.Should().Be(ChooserPhase.Idle);
        chooser.Fingers.Should().BeEmpty();
    }

    [TestMethod]
    public void Fingers_GetFreeColours()
    {
        var chooser = Create();
        Down(chooser, 1, 2, 3);
        chooser.TouchUp(2);

        Down(chooser, 4);

        chooser.Fingers.Select(f => f.ColorIndex).Should().BeEquivalentTo([0, 2, 1]);
    }

    [TestMethod]
    public void TouchDown_IgnoresKnownIdsAndMoreFingersThanColours()
    {
        var chooser = Create();
        Down(chooser, Enumerable.Range(1, FingerChooser.PaletteSize).Select(i => (long)i).ToArray());

        chooser.TouchDown(1, 0, 0).Should().BeFalse();
        chooser.TouchDown(99, 0, 0).Should().BeFalse();
        chooser.Fingers.Should().HaveCount(FingerChooser.PaletteSize);
    }

    [TestMethod]
    public void TouchMove_UpdatesThePosition()
    {
        var chooser = Create();
        Down(chooser, 1);

        chooser.TouchMove(1, 123, 456);

        chooser.Fingers[0].X.Should().Be(123);
        chooser.Fingers[0].Y.Should().Be(456);
    }

    [TestMethod]
    [DataRow(ChooserMode.Winners, 0, 1)]
    [DataRow(ChooserMode.Winners, 20, FingerChooser.MaxWinners)]
    [DataRow(ChooserMode.Teams, 1, 2)]
    [DataRow(ChooserMode.Teams, 20, FingerChooser.MaxTeams)]
    public void Configure_ClampsTheCount(ChooserMode mode, int count, int expected) =>
        Create(mode, count).Count.Should().Be(expected);

    [TestMethod]
    public void Configure_OnlyBetweenRounds()
    {
        var chooser = Create();
        Down(chooser, 1);

        chooser.Configure(ChooserMode.Teams, 3).Should().BeFalse();
        chooser.Mode.Should().Be(ChooserMode.Winners);
    }

    [TestMethod]
    public void ViewModel_ShowsTheCountAndStatus_AndGivesHaptics()
    {
        // Arrange
        var haptics = Substitute.For<IHaptics>();
        var viewModel = new ChooserViewModel(haptics, _time);

        // Act / Assert
        viewModel.CountText.Should().Be("1 winnaar");
        viewModel.DecrementCommand.CanExecute(null).Should().BeFalse();
        viewModel.IncrementCommand.Execute(null);
        viewModel.CountText.Should().Be("2 winnaars");

        viewModel.SetModeCommand.Execute(ChooserMode.Teams);
        viewModel.CountText.Should().Be("2 teams");
        viewModel.DecrementCommand.CanExecute(null).Should().BeFalse();

        viewModel.TouchDown(1, 0, 0);
        viewModel.IsIdle.Should().BeFalse();
        viewModel.Status.Should().Be("Nog 1 vinger nodig");
        viewModel.TouchDown(2, 0, 0);
        _time.Advance(FingerChooser.CountdownDuration);
        viewModel.Tick();
        viewModel.Chooser.Phase.Should().Be(ChooserPhase.Decided);
        haptics.Received(1).Success();

        viewModel.TouchUp(1);
        viewModel.TouchUp(2);
        viewModel.IsIdle.Should().BeTrue();
    }
}
