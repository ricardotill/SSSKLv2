namespace SSSKL.Mobile.Core.Services;

/// <summary>The app's colour mode. Stored with the same values as the SPA (theme.service.ts), so both agree.</summary>
public enum ThemeMode
{
    Auto,
    Light,
    Dark
}

/// <summary>Applies a colour mode to the native UI (MAUI <c>Application.UserAppTheme</c> in the app).</summary>
public interface IThemeApplier
{
    void Apply(ThemeMode mode);
}

/// <summary>The colour mode the user picked in the native settings. Singleton.</summary>
public sealed class ThemePreference(ISettingsStore store, IThemeApplier applier)
{
    public const string StoreKey = "theme_mode";

    public ThemeMode Mode => Parse(store.Get(StoreKey));

    /// <summary>Raised after the mode changed and was applied natively; WebViews reload with the new mode.</summary>
    public event EventHandler<ThemeMode>? Changed;

    /// <summary>Applies the stored mode; call once at startup.</summary>
    public void ApplyStored() => applier.Apply(Mode);

    public void Set(ThemeMode mode)
    {
        if (mode == Mode)
        {
            return;
        }
        store.Set(StoreKey, ToWebValue(mode));
        applier.Apply(mode);
        Changed?.Invoke(this, mode);
    }

    /// <summary>The SPA's value for the mode ('auto', 'light' or 'dark').</summary>
    public static string ToWebValue(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => "light",
        ThemeMode.Dark => "dark",
        _ => "auto"
    };

    public static ThemeMode Parse(string? value) => value switch
    {
        "light" => ThemeMode.Light,
        "dark" => ThemeMode.Dark,
        _ => ThemeMode.Auto
    };
}
