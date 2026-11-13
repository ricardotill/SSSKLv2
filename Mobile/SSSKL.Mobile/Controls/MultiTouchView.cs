using Microsoft.Maui.Handlers;

namespace SSSKL.Mobile.Controls;

/// <summary>A touch on a <see cref="MultiTouchView"/>: the platform's id for it and its position in device-independent units.</summary>
public readonly record struct TouchPoint(long Id, float X, float Y);

/// <summary>
/// A <see cref="GraphicsView"/> that reports every finger separately (MAUI's own touch events only follow
/// one). The platform code (<see cref="MultiTouchPlatform"/>, Platforms/*/MultiTouchPlatform.*.cs) is hooked
/// into the GraphicsView handler in <c>MauiProgram</c>. A cancelled touch is reported as <see cref="TouchUp"/>.
/// </summary>
public sealed class MultiTouchView : GraphicsView
{
    public event EventHandler<TouchPoint>? TouchDown;
    public event EventHandler<TouchPoint>? TouchMove;
    public event EventHandler<TouchPoint>? TouchUp;

    internal void OnTouchDown(long id, float x, float y) => TouchDown?.Invoke(this, new TouchPoint(id, x, y));
    internal void OnTouchMove(long id, float x, float y) => TouchMove?.Invoke(this, new TouchPoint(id, x, y));
    internal void OnTouchUp(long id) => TouchUp?.Invoke(this, new TouchPoint(id, 0, 0));
}

public static partial class MultiTouchPlatform
{
    public static partial void Connect(IGraphicsViewHandler handler, MultiTouchView view);
}
