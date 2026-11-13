using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SSSKL.Mobile.Core.Chooser;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// "Vingeren": the mode and count controls around a <see cref="FingerChooser"/>. The page forwards the touches
/// and calls <see cref="Tick"/> every frame while <see cref="IsActive"/>; the drawable reads <see cref="Chooser"/>.
/// </summary>
public sealed partial class ChooserViewModel : ObservableObject
{
    private readonly IHaptics _haptics;

    public ChooserViewModel(IHaptics haptics, TimeProvider time)
    {
        _haptics = haptics;
        Chooser = new FingerChooser(time, Random.Shared);
        Chooser.FingerAdded += (_, _) => _haptics.Selection();
        Chooser.Decided += (_, _) => _haptics.Success();
        Update();
    }

    public FingerChooser Chooser { get; }

    [ObservableProperty]
    public partial ChooserMode Mode { get; private set; }

    [ObservableProperty]
    public partial string CountText { get; private set; } = "";

    [ObservableProperty]
    public partial string Status { get; private set; } = "";

    /// <summary>No fingers down: the controls show.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    public partial bool IsIdle { get; private set; } = true;

    public bool IsActive => !IsIdle;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DecrementCommand))]
    public partial bool CanDecrement { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IncrementCommand))]
    public partial bool CanIncrement { get; private set; }

    [RelayCommand]
    private void SetMode(ChooserMode mode)
    {
        if (mode == Chooser.Mode)
        {
            return;
        }
        // Keep the count when it fits the new mode (1 winner becomes 2 teams).
        if (Chooser.Configure(mode, Chooser.Count))
        {
            _haptics.Selection();
            Update();
        }
    }

    [RelayCommand(CanExecute = nameof(CanIncrement))]
    private void Increment() => ChangeCount(+1);

    [RelayCommand(CanExecute = nameof(CanDecrement))]
    private void Decrement() => ChangeCount(-1);

    public void TouchDown(long id, float x, float y)
    {
        if (Chooser.TouchDown(id, x, y))
        {
            Update();
        }
    }

    public void TouchMove(long id, float x, float y) => Chooser.TouchMove(id, x, y);

    public void TouchUp(long id)
    {
        Chooser.TouchUp(id);
        Update();
    }

    /// <summary>One frame: ends the countdown when it ran out.</summary>
    public void Tick()
    {
        if (Chooser.Tick())
        {
            Update();
        }
    }

    /// <summary>The page went away mid-round: start over next time.</summary>
    public void Reset()
    {
        Chooser.Reset();
        Update();
    }

    private void ChangeCount(int delta)
    {
        if (Chooser.Configure(Chooser.Mode, Chooser.Count + delta))
        {
            _haptics.Selection();
            Update();
        }
    }

    private void Update()
    {
        var winners = Chooser.Mode == ChooserMode.Winners;
        Mode = Chooser.Mode;
        CountText = winners ? Texts.WinnerCount(Chooser.Count) : Texts.TeamCount(Chooser.Count);
        CanDecrement = Chooser.Count > (winners ? FingerChooser.MinWinners : FingerChooser.MinTeams);
        CanIncrement = Chooser.Count < (winners ? FingerChooser.MaxWinners : FingerChooser.MaxTeams);
        IsIdle = Chooser.Phase == ChooserPhase.Idle;
        Status = Chooser.Phase switch
        {
            ChooserPhase.Waiting => Texts.ChooserNeedMore(Chooser.RequiredFingers - Chooser.Fingers.Count),
            ChooserPhase.Counting => Texts.ChooserHold,
            ChooserPhase.Decided => Texts.ChooserLift,
            _ => Texts.ChooserHint
        };
    }
}
