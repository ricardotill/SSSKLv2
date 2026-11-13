namespace SSSKL.Mobile.Core.Chooser;

/// <summary>What <see cref="FingerChooser"/> picks: winners among the fingers, or a team for every finger.</summary>
public enum ChooserMode
{
    Winners,
    Teams
}

public enum ChooserPhase
{
    /// <summary>No fingers on the screen.</summary>
    Idle,

    /// <summary>Fingers on the screen, but fewer than <see cref="FingerChooser.RequiredFingers"/>.</summary>
    Waiting,

    /// <summary>Enough fingers: the countdown runs; adding or lifting a finger restarts it.</summary>
    Counting,

    /// <summary>The result is shown until every finger is lifted.</summary>
    Decided
}

/// <summary>A finger on the screen, in device-independent units.</summary>
public sealed class Finger
{
    internal Finger(long id, float x, float y, int colorIndex, DateTimeOffset downAt)
    {
        Id = id;
        X = x;
        Y = y;
        ColorIndex = colorIndex;
        DownAt = downAt;
    }

    /// <summary>The platform's id for the touch.</summary>
    public long Id { get; }
    public float X { get; internal set; }
    public float Y { get; internal set; }

    /// <summary>A palette slot (0..<see cref="FingerChooser.PaletteSize"/>-1) no other finger on the screen has.</summary>
    public int ColorIndex { get; }

    public DateTimeOffset DownAt { get; }

    /// <summary>Lifted after the decision: still shown (the result stays until every finger is lifted).</summary>
    public bool IsLifted { get; internal set; }

    /// <summary>Chosen in <see cref="ChooserMode.Winners"/>.</summary>
    public bool IsWinner { get; internal set; }

    /// <summary>The team (0-based) in <see cref="ChooserMode.Teams"/>; -1 before the decision.</summary>
    public int Team { get; internal set; } = -1;
}

/// <summary>
/// "Vingeren" (like Chwazi): everyone puts a finger on the screen; once enough fingers have stayed put for
/// <see cref="CountdownDuration"/>, it picks <see cref="Count"/> winners or splits the fingers into
/// <see cref="Count"/> balanced teams. No UI: the page feeds it touches and calls <see cref="Tick"/> per frame.
/// </summary>
public sealed class FingerChooser(TimeProvider time, Random random)
{
    public static readonly TimeSpan CountdownDuration = TimeSpan.FromSeconds(3);
    public const int PaletteSize = 10;
    public const int MinWinners = 1;
    public const int MaxWinners = 9;
    public const int MinTeams = 2;
    public const int MaxTeams = 5;

    private readonly List<Finger> _fingers = [];
    private DateTimeOffset _countdownStart;

    /// <summary>A finger was added (not after the decision).</summary>
    public event EventHandler<Finger>? FingerAdded;

    /// <summary>The countdown ended and the result is set on the fingers.</summary>
    public event EventHandler? Decided;

    public ChooserMode Mode { get; private set; } = ChooserMode.Winners;
    public int Count { get; private set; } = 1;
    public ChooserPhase Phase { get; private set; } = ChooserPhase.Idle;
    public DateTimeOffset? DecidedAt { get; private set; }
    public IReadOnlyList<Finger> Fingers => _fingers;

    public DateTimeOffset Now => time.GetUtcNow();

    /// <summary>Fingers needed before the countdown starts: one more than the winners, or one per team.</summary>
    public int RequiredFingers => Mode == ChooserMode.Winners ? Count + 1 : Count;

    /// <summary>The countdown's progress (0..1); 0 outside <see cref="ChooserPhase.Counting"/>.</summary>
    public double Progress => Phase == ChooserPhase.Counting
        ? Math.Clamp((Now - _countdownStart) / CountdownDuration, 0, 1)
        : 0;

    /// <summary>Sets the mode and count (clamped to the mode's range). Only while <see cref="ChooserPhase.Idle"/>.</summary>
    public bool Configure(ChooserMode mode, int count)
    {
        if (Phase != ChooserPhase.Idle)
        {
            return false;
        }
        Mode = mode;
        Count = mode == ChooserMode.Winners
            ? Math.Clamp(count, MinWinners, MaxWinners)
            : Math.Clamp(count, MinTeams, MaxTeams);
        return true;
    }

    /// <summary>Returns false when the finger is ignored (after the decision, a known id, or no free colour).</summary>
    public bool TouchDown(long id, float x, float y)
    {
        if (Phase == ChooserPhase.Decided || _fingers.Exists(f => f.Id == id) || _fingers.Count >= PaletteSize)
        {
            return false;
        }
        var colorIndex = Enumerable.Range(0, PaletteSize).First(i => !_fingers.Exists(f => f.ColorIndex == i));
        var finger = new Finger(id, x, y, colorIndex, Now);
        _fingers.Add(finger);
        UpdateCountdown();
        FingerAdded?.Invoke(this, finger);
        return true;
    }

    public void TouchMove(long id, float x, float y)
    {
        if (Find(id) is { IsLifted: false } finger)
        {
            finger.X = x;
            finger.Y = y;
        }
    }

    /// <summary>A finger was lifted (or the system cancelled the touch).</summary>
    public void TouchUp(long id)
    {
        if (Find(id) is not { } finger)
        {
            return;
        }
        if (Phase == ChooserPhase.Decided)
        {
            finger.IsLifted = true;
            if (_fingers.TrueForAll(f => f.IsLifted))
            {
                Reset();
            }
            return;
        }
        _fingers.Remove(finger);
        UpdateCountdown();
    }

    /// <summary>Ends the countdown when it has run out. Returns true when it decided.</summary>
    public bool Tick()
    {
        if (Phase != ChooserPhase.Counting || Now - _countdownStart < CountdownDuration)
        {
            return false;
        }
        Decide();
        return true;
    }

    /// <summary>Forgets every finger (e.g. when the page disappears mid-game).</summary>
    public void Reset()
    {
        _fingers.Clear();
        DecidedAt = null;
        Phase = ChooserPhase.Idle;
    }

    private Finger? Find(long id) => _fingers.Find(f => f.Id == id);

    /// <summary>Any change in the fingers restarts the countdown, so everyone gets the full time to join.</summary>
    private void UpdateCountdown()
    {
        if (_fingers.Count == 0)
        {
            Phase = ChooserPhase.Idle;
        }
        else if (_fingers.Count < RequiredFingers)
        {
            Phase = ChooserPhase.Waiting;
        }
        else
        {
            Phase = ChooserPhase.Counting;
            _countdownStart = Now;
        }
    }

    private void Decide()
    {
        // Fisher–Yates: every order equally likely.
        var order = _fingers.ToArray();
        for (var i = order.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        for (var i = 0; i < order.Length; i++)
        {
            if (Mode == ChooserMode.Winners)
            {
                order[i].IsWinner = i < Count;
            }
            else
            {
                // Round-robin over a random order: random teams whose sizes differ by at most one.
                order[i].Team = i % Count;
            }
        }

        Phase = ChooserPhase.Decided;
        DecidedAt = Now;
        Decided?.Invoke(this, EventArgs.Empty);
    }
}
