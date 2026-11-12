namespace SSSKL.Mobile.Services;

/// <summary>
/// While a page on the brand colour is shown (<see cref="Views.WelcomePage"/>), the system bars get light icons
/// in both colour modes, and on Android before 15 the brand colour. Otherwise they follow the colour mode.
/// </summary>
internal static class BrandSystemBars
{
    public static bool Active { get; private set; }

    public static void Set(bool active)
    {
        Active = active;
#if ANDROID
        if (Platform.CurrentActivity is { } activity)
        {
            SystemBars.Apply(activity);
        }
#elif IOS
        // Info.plist turns view-controller-based status bar appearance off, so this style applies app-wide;
        // Default follows the window's colour mode.
#pragma warning disable CA1422, CS0618
        UIKit.UIApplication.SharedApplication.SetStatusBarStyle(
            active ? UIKit.UIStatusBarStyle.LightContent : UIKit.UIStatusBarStyle.Default, true);
#pragma warning restore CA1422, CS0618
#endif
    }
}
