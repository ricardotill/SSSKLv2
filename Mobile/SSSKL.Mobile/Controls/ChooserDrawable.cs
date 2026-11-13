using SSSKL.Mobile.Core.Chooser;

namespace SSSKL.Mobile.Controls;

/// <summary>
/// Draws a <see cref="FingerChooser"/> round: a coloured ring per finger that grows in and breathes, a
/// countdown arc while counting, then the result. One winner floods the screen in its colour around a hole
/// over the finger; several winners pulse while the rest fade; teams take their team's colour and number.
/// </summary>
public sealed class ChooserDrawable(FingerChooser chooser) : IDrawable
{
    private const float Radius = 44;
    private static readonly TimeSpan GrowIn = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan Reveal = TimeSpan.FromMilliseconds(600);

    /// <summary>Vivid on light and dark alike; a finger keeps its slot (<see cref="Finger.ColorIndex"/>), a team its index.</summary>
    private static readonly Color[] Palette =
    [
        Color.FromArgb("#EF4444"), Color.FromArgb("#3B82F6"), Color.FromArgb("#22C55E"), Color.FromArgb("#EAB308"),
        Color.FromArgb("#A855F7"), Color.FromArgb("#F97316"), Color.FromArgb("#EC4899"), Color.FromArgb("#14B8A6"),
        Color.FromArgb("#84CC16"), Color.FromArgb("#06B6D4")
    ];

    /// <summary>The page's background, for the hole in the winner's flood.</summary>
    public Color Background { get; set; } = Colors.Black;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var now = chooser.Now;
        var fingers = chooser.Fingers;
        if (fingers.Count == 0)
        {
            return;
        }

        var decided = chooser.Phase == ChooserPhase.Decided;
        var sinceDecision = decided && chooser.DecidedAt is { } at ? (float)((now - at) / Reveal) : 0f;
        var reveal = EaseOut(Math.Clamp(sinceDecision, 0, 1));
        var seconds = (float)(now - DateTimeOffset.UnixEpoch).TotalSeconds;

        if (decided && chooser.Mode == ChooserMode.Winners && chooser.Count == 1 && fingers.FirstOrDefault(f => f.IsWinner) is { } winner)
        {
            DrawFlood(canvas, dirtyRect, winner, reveal);
        }

        foreach (var finger in fingers)
        {
            var grow = EaseOut((float)Math.Clamp((now - finger.DownAt) / GrowIn, 0, 1));
            var breathe = 1 + 0.05f * MathF.Sin(seconds * 6 + finger.ColorIndex);
            var color = Palette[finger.ColorIndex % Palette.Length];
            var radius = Radius * grow * breathe;
            var alpha = 1f;
            string? label = null;

            if (decided)
            {
                if (chooser.Mode == ChooserMode.Winners)
                {
                    if (!finger.IsWinner)
                    {
                        alpha = 1 - reveal;
                    }
                    else
                    {
                        radius = Radius * (1 + 0.2f * reveal) * breathe;
                        if (chooser.Count > 1)
                        {
                            DrawHalo(canvas, finger, color, radius, seconds);
                        }
                    }
                }
                else
                {
                    color = Palette[finger.Team % Palette.Length];
                    // A little pop as the team colour comes in.
                    radius = Radius * (1 + 0.3f * (1 - reveal)) * breathe;
                    label = (finger.Team + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            if (alpha <= 0.01f)
            {
                continue;
            }
            DrawFinger(canvas, finger, color.WithAlpha(alpha), radius, label);

            if (chooser.Phase == ChooserPhase.Counting)
            {
                DrawCountdown(canvas, finger, color, radius, (float)chooser.Progress);
            }
        }
    }

    private static void DrawFinger(ICanvas canvas, Finger finger, Color color, float radius, string? label)
    {
        canvas.FillColor = color;
        canvas.FillCircle(finger.X, finger.Y, radius * 0.62f);
        canvas.StrokeColor = color;
        canvas.StrokeSize = 7;
        canvas.DrawCircle(finger.X, finger.Y, radius);

        if (label is not null)
        {
            canvas.FontColor = Colors.White;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.FontSize = 24;
            canvas.DrawString(label, finger.X - radius, finger.Y - radius, radius * 2, radius * 2,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }

    /// <summary>An arc around the ring that fills clockwise from the top.</summary>
    private static void DrawCountdown(ICanvas canvas, Finger finger, Color color, float radius, float progress)
    {
        if (progress <= 0)
        {
            return;
        }
        var r = radius + 14;
        canvas.StrokeColor = color;
        canvas.StrokeSize = 5;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawArc(finger.X - r, finger.Y - r, r * 2, r * 2, 90, 90 - 360 * progress, clockwise: true, closed: false);
    }

    /// <summary>One of several winners: a soft ring that keeps pulsing outwards.</summary>
    private static void DrawHalo(ICanvas canvas, Finger finger, Color color, float radius, float seconds)
    {
        var wave = seconds * 1.2f % 1;
        canvas.StrokeColor = color.WithAlpha(1 - wave);
        canvas.StrokeSize = 4;
        canvas.DrawCircle(finger.X, finger.Y, radius * (1.2f + wave));
    }

    /// <summary>The winner's colour fills the screen, closing in on a hole around the finger.</summary>
    private void DrawFlood(ICanvas canvas, RectF rect, Finger winner, float reveal)
    {
        var far = MathF.Sqrt(rect.Width * rect.Width + rect.Height * rect.Height);
        var hole = far + (Radius * 2.2f - far) * reveal;
        canvas.FillColor = Palette[winner.ColorIndex % Palette.Length];
        canvas.FillRectangle(rect);
        canvas.FillColor = Background;
        canvas.FillCircle(winner.X, winner.Y, hole);
    }

    private static float EaseOut(float t) => 1 - (1 - t) * (1 - t) * (1 - t);
}
