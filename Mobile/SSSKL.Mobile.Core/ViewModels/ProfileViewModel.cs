using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

public sealed record AchievementItem(string Name, string Description, string? ImageUrl, string Tier, DateTime DateAdded, Guid AchievementId = default)
{
    public bool HasImage => ImageUrl is not null;
}

/// <summary>
/// Your own profile: picture, saldo, stats, description and achievements. The description is edited as plain
/// text (see <see cref="DescriptionText"/>); name and phone number are in the native settings.
/// </summary>
public sealed partial class ProfileViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IUserDialogs dialogs,
    IPhotoPicker photoPicker,
    IAppNavigator navigator,
    AppEndpoints endpoints,
    ILogger<ProfileViewModel> logger) : ObservableObject
{
    /// <summary>Profile pictures above this size are rejected client-side, like the SPA does.</summary>
    public const int MaxPictureBytes = 5 * 1024 * 1024;

    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");

    public ObservableCollection<AchievementItem> Achievements { get; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string DisplayName { get; set; } = "";

    [ObservableProperty]
    public partial string UserName { get; set; } = "";

    [ObservableProperty]
    public partial string Saldo { get; set; } = "";

    [ObservableProperty]
    public partial bool IsSaldoNegative { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPicture))]
    public partial string? PictureUrl { get; set; }

    [ObservableProperty]
    public partial string Initials { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoDescription))]
    public partial string? DescriptionHtml { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoDescription))]
    public partial bool IsEditingDescription { get; set; }

    [ObservableProperty]
    public partial string DescriptionDraft { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveDescriptionCommand))]
    public partial bool IsSavingDescription { get; set; }

    public bool HasNoDescription => DescriptionHtml is null && !IsEditingDescription;

    [ObservableProperty]
    public partial int TotalOrders { get; set; }

    [ObservableProperty]
    public partial int CurrentStreak { get; set; }

    [ObservableProperty]
    public partial bool HasNoAchievements { get; set; }

    public bool HasPicture => PictureUrl is not null;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var user = await currentUser.RefreshAsync();
            ApplyUser(user);

            var statsTask = api.GetStatsAsync(user.Id);
            var achievementsTask = api.GetAchievementEntriesAsync(user.Id);
            await Task.WhenAll(statsTask, achievementsTask);

            TotalOrders = statsTask.Result.TotalOrders;
            CurrentStreak = statsTask.Result.CurrentStreak;

            Achievements.Clear();
            foreach (var entry in achievementsTask.Result.OrderByDescending(a => a.DateAdded))
            {
                Achievements.Add(new AchievementItem(entry.AchievementName, entry.AchievementDescription,
                    endpoints.ResolveApiUrl(entry.ImageUrl), entry.Tier, entry.DateAdded, entry.AchievementId));
            }
            HasNoAchievements = Achievements.Count == 0;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Loading profile failed");
            await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task ChangePictureAsync()
    {
        PickedPhoto? photo;
        try
        {
            photo = await photoPicker.PickAsync();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            // MAUI's PermissionException / FeatureNotSupportedException, image decode errors, ... (Core has no
            // MAUI reference, so catch broadly): an unhandled exception here would crash the app.
            logger.LogWarning(ex, "Picking a profile picture failed");
            await dialogs.AlertAsync(Texts.Error, Texts.ProfilePictureFailed);
            return;
        }
        if (photo is null)
        {
            return;
        }
        if (photo.Content.Length > MaxPictureBytes)
        {
            await dialogs.AlertAsync(Texts.Error, Texts.ProfilePictureFailed);
            return;
        }

        await RunBusyAsync(async () =>
        {
            await api.UploadProfilePictureAsync(new Base64FileUpload(photo.FileName, photo.ContentType, Convert.ToBase64String(photo.Content)));
            ApplyUser(await currentUser.RefreshAsync());
            await dialogs.ToastAsync(Texts.ProfilePictureUpdated);
        });
    }

    [RelayCommand]
    private async Task RemovePictureAsync()
    {
        if (!await dialogs.ConfirmAsync(Texts.RemoveProfilePicture, Texts.ConfirmRemoveProfilePicture, Texts.RemoveProfilePicture, Texts.Cancel))
        {
            return;
        }
        await RunBusyAsync(async () =>
        {
            await api.DeleteProfilePictureAsync();
            ApplyUser(await currentUser.RefreshAsync());
            await dialogs.ToastAsync(Texts.ProfilePictureRemoved);
        });
    }

    [RelayCommand]
    private void EditDescription()
    {
        DescriptionDraft = DescriptionText.ToPlainText(DescriptionHtml);
        IsEditingDescription = true;
    }

    [RelayCommand]
    private void CancelEditDescription() => IsEditingDescription = false;

    private bool CanSaveDescription() => !IsSavingDescription;

    [RelayCommand(CanExecute = nameof(CanSaveDescription))]
    private async Task SaveDescriptionAsync()
    {
        // Nothing typed: leave the stored HTML (and any formatting in it) alone.
        if (DescriptionDraft.Trim() == DescriptionText.ToPlainText(DescriptionHtml))
        {
            IsEditingDescription = false;
            return;
        }
        if (DescriptionText.HasRichFormatting(DescriptionHtml) &&
            !await dialogs.ConfirmAsync(Texts.DescriptionLosesFormattingTitle, Texts.DescriptionLosesFormatting, Texts.SaveAnyway, Texts.Cancel))
        {
            return;
        }

        IsSavingDescription = true;
        try
        {
            var html = DescriptionText.ToHtml(DescriptionDraft);
            await api.UpdateMeAsync(new SelfUpdate(null, null, null, html));
            DescriptionHtml = string.IsNullOrWhiteSpace(html) ? null : html;
            IsEditingDescription = false;
            await dialogs.ToastAsync(Texts.DescriptionSaved);
            await RefreshUserQuietlyAsync();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Saving the description failed");
            await dialogs.AlertAsync(Texts.Error, Texts.DescriptionSaveFailed);
        }
        finally
        {
            IsSavingDescription = false;
        }
    }

    [RelayCommand]
    private Task OpenAsync(string path) => navigator.OpenAsync(path);

    [RelayCommand]
    private Task OpenAchievementAsync(AchievementItem achievement) => navigator.OpenAsync($"achievements/{achievement.AchievementId}");

    [RelayCommand]
    private Task OpenSettingsAsync() => navigator.OpenSettingsAsync();

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (await dialogs.ConfirmAsync(Texts.Logout, Texts.ConfirmLogout, Texts.Logout, Texts.Cancel))
        {
            await navigator.SignOutAsync();
        }
    }

    private void ApplyUser(CurrentUser user)
    {
        DisplayName = user.DisplayName;
        UserName = user.UserName;
        Saldo = user.Saldo.ToString("C", Dutch);
        IsSaldoNegative = user.Saldo < 0;
        // The URL stays the same after a new upload, so bust the image cache.
        PictureUrl = user.ProfilePictureUrl is null
            ? null
            : $"{endpoints.ResolveApiUrl(user.ProfilePictureUrl)}{(user.ProfilePictureUrl.Contains('?') ? '&' : '?')}v={DateTime.UtcNow.Ticks}";
        Initials = UserItemViewModel.InitialsOf(user.DisplayName);
        DescriptionHtml = string.IsNullOrWhiteSpace(user.Description) ? null : user.Description;
    }

    /// <summary>Keeps the shared user (Meer, POS) in step; the page already shows the saved description.</summary>
    private async Task RefreshUserQuietlyAsync()
    {
        try
        {
            await currentUser.RefreshAsync();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Refreshing the current user failed");
        }
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Updating profile picture failed");
            await dialogs.AlertAsync(Texts.Error, ex is ApiException api ? api.Message : Texts.ProfilePictureFailed);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
