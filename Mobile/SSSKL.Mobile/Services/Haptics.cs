using SSSKL.Mobile.Core.Services;
#if IOS
using UIKit;
#endif

namespace SSSKL.Mobile.Services;

/// <summary>
/// Haptic feedback: iOS's selection and notification generators, Android's view haptics (CONFIRM/REJECT where
/// available, falling back to MAUI's click). Never throws: no haptics is never worth an error.
/// </summary>
public sealed class Haptics : IHaptics
{
    public void Selection() => Run(() =>
    {
#if IOS
        using var generator = new UISelectionFeedbackGenerator();
        generator.SelectionChanged();
#else
        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
#endif
    });

    public void Success() => Run(() =>
    {
#if IOS
        using var generator = new UINotificationFeedbackGenerator();
        generator.NotificationOccurred(UINotificationFeedbackType.Success);
#elif ANDROID
        PerformAndroid(success: true);
#endif
    });

    public void Error() => Run(() =>
    {
#if IOS
        using var generator = new UINotificationFeedbackGenerator();
        generator.NotificationOccurred(UINotificationFeedbackType.Error);
#elif ANDROID
        PerformAndroid(success: false);
#endif
    });

#if ANDROID
    /// <summary>CONFIRM/REJECT exist from Android 11 (API 30); older versions get a long press.</summary>
    private static void PerformAndroid(bool success)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            return;
        }
        var view = Platform.CurrentActivity?.Window?.DecorView;
        view?.PerformHapticFeedback(success ? Android.Views.FeedbackConstants.Confirm : Android.Views.FeedbackConstants.Reject);
    }
#endif

    private static void Run(Action action)
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => Run(action));
            return;
        }
        try
        {
            action();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Haptics failed: {ex.Message}");
        }
    }
}
