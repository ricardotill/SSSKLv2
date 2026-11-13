using Android.Views;
using Microsoft.Maui.Handlers;

namespace SSSKL.Mobile.Controls;

public static partial class MultiTouchPlatform
{
    /// <summary>
    /// A touch listener on the native view: it sees every pointer (MotionEvent pointer ids) before MAUI's own
    /// single-touch handling, which it replaces. Positions are converted from pixels to device-independent units.
    /// </summary>
    public static partial void Connect(IGraphicsViewHandler handler, MultiTouchView view)
    {
        var native = handler.PlatformView;
        var density = native.Context?.Resources?.DisplayMetrics?.Density ?? 1f;

        native.Touch += (_, e) =>
        {
            if (e.Event is not { } ev)
            {
                return;
            }
            e.Handled = true;
            switch (ev.ActionMasked)
            {
                case MotionEventActions.Down:
                case MotionEventActions.PointerDown:
                {
                    // Keep scrolling parents (and the Shell) from taking over the gesture.
                    native.Parent?.RequestDisallowInterceptTouchEvent(true);
                    var index = ev.ActionIndex;
                    view.OnTouchDown(ev.GetPointerId(index), ev.GetX(index) / density, ev.GetY(index) / density);
                    break;
                }
                case MotionEventActions.Move:
                    for (var i = 0; i < ev.PointerCount; i++)
                    {
                        view.OnTouchMove(ev.GetPointerId(i), ev.GetX(i) / density, ev.GetY(i) / density);
                    }
                    break;
                case MotionEventActions.Up:
                case MotionEventActions.PointerUp:
                    view.OnTouchUp(ev.GetPointerId(ev.ActionIndex));
                    break;
                case MotionEventActions.Cancel:
                    for (var i = 0; i < ev.PointerCount; i++)
                    {
                        view.OnTouchUp(ev.GetPointerId(i));
                    }
                    break;
            }
        };
    }
}
