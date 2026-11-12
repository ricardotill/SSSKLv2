using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;

namespace SSSKL.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SystemBars.Apply(this);
        if (Microsoft.Maui.Controls.Application.Current is { } app)
        {
            // Also fires when the colour mode is picked in the app (UserAppTheme), not only by the system.
            app.RequestedThemeChanged += OnThemeChanged;
        }
    }

    protected override void OnDestroy()
    {
        if (Microsoft.Maui.Controls.Application.Current is { } app)
        {
            app.RequestedThemeChanged -= OnThemeChanged;
        }
        base.OnDestroy();
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        SystemBars.Apply(this);
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => RunOnUiThread(() => SystemBars.Apply(this));
}
