using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Services;

/// <summary>Applies the colour mode to the whole app (native pages; WebViews follow through the SPA bridge).</summary>
public sealed class ThemeApplier : IThemeApplier
{
    public void Apply(ThemeMode mode) => MainThread.BeginInvokeOnMainThread(() =>
    {
        if (Application.Current is { } app)
        {
            app.UserAppTheme = ToAppTheme(mode);
        }
    });

    public static AppTheme ToAppTheme(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => AppTheme.Light,
        ThemeMode.Dark => AppTheme.Dark,
        _ => AppTheme.Unspecified
    };
}
