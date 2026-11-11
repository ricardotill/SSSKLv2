using SSSKL.Mobile.Core.Models;

namespace SSSKL.Mobile.Core.Services;

/// <summary>Small key/value store (MAUI Preferences in the app).</summary>
public interface ISettingsStore
{
    string? Get(string key);
    void Set(string key, string value);
    void Remove(string key);
}

/// <summary>Shows the native popups (CommunityToolkit.Maui popups in the app).</summary>
public interface IPopupPresenter
{
    Task ShowAchievementsAsync(IReadOnlyList<AchievementEntry> entries);
    Task ShowWhatsNewAsync(string html);
}

/// <summary>Navigation that view models trigger.</summary>
public interface IAppNavigator
{
    /// <summary>
    /// Opens an SPA route (a menu entry, a notification link): natively when <see cref="Navigation.NativeRoutes"/>
    /// knows it, otherwise in the WebView.
    /// </summary>
    Task OpenAsync(string path);

    /// <summary>Opens an SPA route (e.g. <c>/settings</c>) in the "Meer" WebView tab.</summary>
    Task OpenWebAsync(string path);

    /// <summary>Opens the native settings page on top of the current tab.</summary>
    Task OpenSettingsAsync();

    /// <summary>Switches to the "Profiel" tab.</summary>
    Task OpenProfileAsync();

    /// <summary>Signs out locally and shows the login page.</summary>
    Task SignOutAsync();
}

public interface IUserDialogs
{
    Task ToastAsync(string message);
    Task AlertAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}

public sealed record PickedPhoto(string FileName, string ContentType, byte[] Content);

public interface IPhotoPicker
{
    /// <summary>Returns null when the user cancels.</summary>
    Task<PickedPhoto?> PickAsync();
}
