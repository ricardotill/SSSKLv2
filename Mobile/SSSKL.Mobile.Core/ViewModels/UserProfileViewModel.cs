using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// Another member's profile (the SPA's profile drawer): picture, name, description and their latest
/// achievements. Admins also see their orders and streak and can open their statistics, as the API only
/// gives statistics to the member themselves and admins. Your own profile is the Profiel tab.
/// </summary>
public sealed partial class UserProfileViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    ILogger<UserProfileViewModel> logger) : ObservableObject
{
    public const int RecentAchievementCount = 4;

    private string _userId = "";

    public ObservableCollection<AchievementChip> RecentAchievements { get; } = [];

    [ObservableProperty]
    public partial string DisplayName { get; set; } = "";

    [ObservableProperty]
    public partial string UserName { get; set; } = "";

    [ObservableProperty]
    public partial string Initials { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPicture))]
    public partial string? PictureUrl { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDescription))]
    public partial string? DescriptionHtml { get; set; }

    [ObservableProperty]
    public partial bool IsAdmin { get; set; }

    [ObservableProperty]
    public partial string TotalOrders { get; set; } = "";

    [ObservableProperty]
    public partial string CurrentStreak { get; set; } = "";

    [ObservableProperty]
    public partial bool HasStats { get; set; }

    [ObservableProperty]
    public partial bool HasNoAchievements { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool HasLoaded { get; set; }

    public bool HasPicture => PictureUrl is not null;
    public bool HasDescription => DescriptionHtml is not null;

    public void Initialize(string userId) => _userId = userId;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var me = currentUser.User ?? await currentUser.RefreshAsync();
            IsAdmin = me.IsInRole(RoleNames.Admin);

            var profileTask = api.GetUserProfileAsync(_userId);
            var achievementsTask = TryAsync(() => api.GetAchievementEntriesAsync(_userId), (IReadOnlyList<AchievementEntry>)[]);
            var statsTask = IsAdmin ? TryAsync<UserStats?>(async () => await api.GetStatsAsync(_userId), null) : Task.FromResult<UserStats?>(null);
            var profile = await profileTask;
            var achievements = await achievementsTask;
            var stats = await statsTask;

            DisplayName = profile.DisplayName;
            UserName = "@" + profile.UserName;
            Initials = UserItemViewModel.InitialsOf(profile.DisplayName);
            PictureUrl = endpoints.ResolveApiUrl(profile.ProfilePictureUrl);
            DescriptionHtml = string.IsNullOrWhiteSpace(profile.Description) ? null : profile.Description;

            RecentAchievements.Clear();
            foreach (var entry in achievements.OrderByDescending(a => a.DateAdded).Take(RecentAchievementCount))
            {
                RecentAchievements.Add(new AchievementChip(entry.AchievementId, entry.AchievementName, endpoints.ResolveApiUrl(entry.ImageUrl), entry.Tier));
            }
            HasNoAchievements = RecentAchievements.Count == 0;

            HasStats = stats is not null;
            if (stats is not null)
            {
                TotalOrders = Display.Number(stats.TotalOrders);
                CurrentStreak = Texts.Days(stats.CurrentStreak);
            }
            HasLoaded = true;
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading the profile of {UserId} failed", _userId);
            await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task OpenAchievementAsync(AchievementChip chip) => navigator.OpenAsync($"achievements/{chip.AchievementId}");

    [RelayCommand]
    private Task OpenAchievementsAsync() => navigator.OpenAsync($"achievements?userId={Uri.EscapeDataString(_userId)}");

    [RelayCommand]
    private Task OpenStatsAsync() => navigator.OpenAsync($"stats?userId={Uri.EscapeDataString(_userId)}");

    private async Task<T> TryAsync<T>(Func<Task<T>> call, T fallback)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading part of the profile of {UserId} failed", _userId);
            return fallback;
        }
    }

    private static bool IsExpected(Exception ex) =>
        ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException;
}
