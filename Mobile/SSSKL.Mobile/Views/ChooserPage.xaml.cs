using SSSKL.Mobile.Controls;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

/// <summary>
/// "Vingeren": forwards every finger from <see cref="MultiTouchView"/> to the view model and redraws about 60
/// times a second while a round runs. The screen stays on while the page shows.
/// </summary>
public partial class ChooserPage : ContentPage
{
    private readonly ChooserViewModel _viewModel;
    private readonly ChooserDrawable _drawable;
    private readonly IDispatcherTimer _timer;

    public ChooserPage(ChooserViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        Canvas.Drawable = _drawable = new ChooserDrawable(viewModel.Chooser);

        Canvas.TouchDown += OnTouchDown;
        Canvas.TouchMove += (_, t) => _viewModel.TouchMove(t.Id, t.X, t.Y);
        Canvas.TouchUp += (_, t) =>
        {
            _viewModel.TouchUp(t.Id);
            Canvas.Invalidate();
        };

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnFrame;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        DeviceDisplay.Current.KeepScreenOn = true;
        SetSwipeBack(enabled: false);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;
        SetSwipeBack(enabled: true);
        _timer.Stop();
        _viewModel.Reset();
        Canvas.Invalidate();
    }

    private void OnTouchDown(object? sender, TouchPoint touch)
    {
        if (!_timer.IsRunning)
        {
            _drawable.Background = PageBackground();
            _timer.Start();
        }
        _viewModel.TouchDown(touch.Id, touch.X, touch.Y);
        Canvas.Invalidate();
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        _viewModel.Tick();
        Canvas.Invalidate();
        if (_viewModel.IsIdle)
        {
            // The last frame cleared the canvas.
            _timer.Stop();
        }
    }

    private static Color PageBackground()
    {
        var resources = Application.Current!.Resources;
        return (Color)resources[Application.Current.RequestedTheme == AppTheme.Dark ? "PageDark" : "PageLight"];
    }

    /// <summary>
    /// iOS: a finger near the left edge would start the swipe back. The back button still works. Android's
    /// system back gesture can't be turned off for a part of the screen this simply.
    /// </summary>
    private void SetSwipeBack(bool enabled)
    {
#if IOS
        if (Handler is IPlatformViewHandler { ViewController.NavigationController.InteractivePopGestureRecognizer: { } recognizer })
        {
            recognizer.Enabled = enabled;
        }
#endif
    }
}
