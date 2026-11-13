using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;

namespace SSSKL.Mobile.Controls;

public static partial class MultiTouchPlatform
{
    /// <summary>
    /// Turns on multi-touch for the native view and watches its touches with <see cref="FingerRecognizer"/>,
    /// which never recognizes a gesture of its own, so it doesn't get in the way of the view's handling.
    /// </summary>
    public static partial void Connect(IGraphicsViewHandler handler, MultiTouchView view)
    {
        var native = handler.PlatformView;
        native.MultipleTouchEnabled = true;
        native.AddGestureRecognizer(new FingerRecognizer(view));
    }

    private sealed class FingerRecognizer : UIGestureRecognizer
    {
        private readonly WeakReference<MultiTouchView> _view;
        private readonly HashSet<long> _active = [];

        public FingerRecognizer(MultiTouchView view)
        {
            _view = new WeakReference<MultiTouchView>(view);
            CancelsTouchesInView = false;
            DelaysTouchesBegan = false;
            DelaysTouchesEnded = false;
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);
            foreach (var touch in touches.ToArray<UITouch>())
            {
                var id = Id(touch);
                _active.Add(id);
                var point = touch.LocationInView(View);
                Target()?.OnTouchDown(id, (float)point.X, (float)point.Y);
            }
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            base.TouchesMoved(touches, evt);
            foreach (var touch in touches.ToArray<UITouch>())
            {
                var point = touch.LocationInView(View);
                Target()?.OnTouchMove(Id(touch), (float)point.X, (float)point.Y);
            }
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);
            End(touches);
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            base.TouchesCancelled(touches, evt);
            End(touches);
        }

        public override void Reset()
        {
            base.Reset();
            // Touches UIKit stopped sending us count as lifted.
            foreach (var id in _active)
            {
                Target()?.OnTouchUp(id);
            }
            _active.Clear();
        }

        private void End(NSSet touches)
        {
            foreach (var touch in touches.ToArray<UITouch>())
            {
                var id = Id(touch);
                _active.Remove(id);
                Target()?.OnTouchUp(id);
            }
            // All fingers gone: fail, so UIKit resets the recognizer for the next sequence.
            if (_active.Count == 0)
            {
                State = UIGestureRecognizerState.Failed;
            }
        }

        private MultiTouchView? Target() => _view.TryGetTarget(out var view) ? view : null;

        /// <summary>A UITouch object stays the same for the touch's whole life.</summary>
        private static long Id(UITouch touch) => touch.Handle.Handle;
    }
}
