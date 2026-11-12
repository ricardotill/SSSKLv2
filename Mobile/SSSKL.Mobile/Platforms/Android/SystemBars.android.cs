using Android.App;
using Android.OS;
using AndroidX.Core.View;

namespace SSSKL.Mobile;

/// <summary>
/// Keeps Android's status and navigation bars in the app's colours and colour mode: the status bar has the
/// page colour, the navigation bar the tab bar's surface, and their icons are dark in light mode and light in
/// dark mode (also when the mode is picked in the app's settings rather than the system's). From Android 15
/// apps are drawn edge-to-edge and the bar colours come from the content behind them; the icon appearance
/// still applies there.
/// </summary>
internal static class SystemBars
{
    public static void Apply(Activity activity)
    {
        if (activity.Window is not { } window || Microsoft.Maui.Controls.Application.Current is not { } app)
        {
            return;
        }
        var dark = app.RequestedTheme == AppTheme.Dark;
        var page = Resource(app, dark ? "PageDark" : "PageLight");
        var surface = Resource(app, dark ? "SurfaceDark" : "SurfaceLight");

        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
        {
#pragma warning disable CA1422 // Ignored from Android 15 (edge-to-edge), still needed before it.
            window.SetStatusBarColor(page);
            window.SetNavigationBarColor(surface);
#pragma warning restore CA1422
        }

        var insets = WindowCompat.GetInsetsController(window, window.DecorView);
        insets.AppearanceLightStatusBars = !dark;
        insets.AppearanceLightNavigationBars = !dark;
    }

    private static Android.Graphics.Color Resource(Microsoft.Maui.Controls.Application app, string key) =>
        app.Resources.TryGetValue(key, out var value) && value is Color color
            ? Android.Graphics.Color.ParseColor(color.ToArgbHex())
            : Android.Graphics.Color.Black;
}
