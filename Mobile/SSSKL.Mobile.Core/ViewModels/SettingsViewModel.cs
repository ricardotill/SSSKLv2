using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// Native settings: name and phone number, colour mode, and the account status. Changing the e-mail
/// address, password, passkeys, 2FA or personal data opens that tab of the SPA's settings page.
/// </summary>
public sealed partial class SettingsViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IUserDialogs dialogs,
    IAppNavigator navigator,
    ThemePreference theme,
    ILogger<SettingsViewModel> logger) : ObservableObject
{
    public const string SecurityPath = "settings?tab=security";
    public const string TwoFactorPath = "settings?tab=twofactor";
    public const string PersonalDataPath = "settings?tab=personaldata";
    public const string AboutPath = "about";

    private (string Name, string Surname, string Phone) _saved = ("", "", "");

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Surname { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string PhoneNumber { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial bool IsSaving { get; set; }

    [ObservableProperty]
    public partial string UserName { get; set; } = "";

    [ObservableProperty]
    public partial string Email { get; set; } = "";

    [ObservableProperty]
    public partial bool IsEmailConfirmed { get; set; }

    [ObservableProperty]
    public partial string EmailStatus { get; set; } = "";

    [ObservableProperty]
    public partial string TwoFactorStatus { get; set; } = "";

    [ObservableProperty]
    public partial string RolesText { get; set; } = "";

    /// <summary>Set by the page (the version comes from MAUI's AppInfo).</summary>
    [ObservableProperty]
    public partial string AppVersion { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuto), nameof(IsLight), nameof(IsDark))]
    public partial ThemeMode Theme { get; set; } = theme.Mode;

    public bool IsAuto => Theme == ThemeMode.Auto;
    public bool IsLight => Theme == ThemeMode.Light;
    public bool IsDark => Theme == ThemeMode.Dark;

    public bool IsDirty =>
        Normalize(Name) != _saved.Name || Normalize(Surname) != _saved.Surname || Normalize(PhoneNumber) != _saved.Phone;

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            if (currentUser.User is { } cached)
            {
                Apply(cached);
            }
            var twoFactorTask = LoadTwoFactorAsync();
            Apply(await currentUser.RefreshAsync());
            await twoFactorTask;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Loading settings failed");
            await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private bool CanSave() => IsDirty && !IsSaving;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            // Like the SPA: an empty field is sent as null, which leaves it unchanged.
            await api.UpdateMeAsync(new SelfUpdate(NullIfEmpty(Name), NullIfEmpty(Surname), NullIfEmpty(PhoneNumber)));
            Apply(await currentUser.RefreshAsync(), overwriteEdits: true);
            await dialogs.ToastAsync(Texts.SettingsSaved);
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Saving settings failed");
            await dialogs.AlertAsync(Texts.Error, Texts.SettingsSaveFailed);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void SetTheme(ThemeMode mode)
    {
        Theme = mode;
        theme.Set(mode);
    }

    [RelayCommand]
    private Task OpenWebAsync(string path) => navigator.OpenWebAsync(path);

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (await dialogs.ConfirmAsync(Texts.Logout, Texts.ConfirmLogout, Texts.Logout, Texts.Cancel))
        {
            await navigator.SignOutAsync();
        }
    }

    private async Task LoadTwoFactorAsync()
    {
        try
        {
            var status = await api.GetTwoFactorStatusAsync();
            TwoFactorStatus = status.IsTwoFactorEnabled ? Texts.On : Texts.Off;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            // Only a status label: leave it empty.
            logger.LogWarning(ex, "Loading the 2FA status failed");
        }
    }

    /// <param name="overwriteEdits">After saving: show what the server stored. Otherwise unsaved typing is kept.</param>
    private void Apply(CurrentUser user, bool overwriteEdits = false)
    {
        var keepEdits = IsDirty && !overwriteEdits;
        _saved = (Normalize(user.Name), Normalize(user.Surname), Normalize(user.PhoneNumber));
        if (!keepEdits)
        {
            Name = _saved.Name;
            Surname = _saved.Surname;
            PhoneNumber = _saved.Phone;
        }
        OnPropertyChanged(nameof(IsDirty));
        SaveCommand.NotifyCanExecuteChanged();
        UserName = user.UserName;
        Email = user.Email ?? "";
        IsEmailConfirmed = user.EmailConfirmed;
        EmailStatus = user.EmailConfirmed ? Texts.EmailConfirmed : Texts.EmailUnconfirmed;
        RolesText = user.Roles.Count == 0 ? Texts.NoRoles : string.Join(", ", user.Roles);
    }

    private static string Normalize(string? value) => value?.Trim() ?? "";

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
