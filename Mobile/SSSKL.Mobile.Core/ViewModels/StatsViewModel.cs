using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

public sealed record StatTile(string Label, string Value, string Icon);

public sealed record StatLine(string Label, string Value)
{
    public string AccessibleText => $"{Label}: {Value}";
}

public sealed record StatSection(string Title, string Icon, IReadOnlyList<StatLine> Lines);

/// <summary>An earned achievement as a small chip (stats, profiles); tapping opens it.</summary>
public sealed record AchievementChip(Guid AchievementId, string Name, string? ImageUrl, string Tier)
{
    public bool HasImage => ImageUrl is not null;
}

/// <summary>
/// "Mijn statistieken" (SPA stats): highlights, three sections of figures and your achievements. Only
/// admins can look at another member's (<c>?userId=</c>), as the API allows. You may recalculate your own
/// statistics once every seven days; admins any time.
/// </summary>
public sealed partial class StatsViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    TimeProvider time,
    ILogger<StatsViewModel> logger) : ObservableObject
{
    public static readonly TimeSpan RecalculateInterval = TimeSpan.FromDays(7);

    private string? _requestedUserId;
    private string? _userId;

    public ObservableCollection<StatTile> Highlights { get; } = [];
    public ObservableCollection<StatSection> Sections { get; } = [];
    public ObservableCollection<AchievementChip> Achievements { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = Texts.MyStats;

    [ObservableProperty]
    public partial string Subtitle { get; set; } = Texts.MyStatsSubtitle;

    [ObservableProperty]
    public partial string? LastRecalculatedText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStats), nameof(ShowSkeleton))]
    public partial bool IsLoading { get; set; }

    /// <summary>The first load failed: the page shows its error state with a retry button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial bool HasLoadError { get; set; }

    /// <summary>Placeholders while the first load runs (not while refreshing loaded statistics).</summary>
    public bool ShowSkeleton => IsLoading && !HasStats && !HasLoadError;

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RecalculateCommand))]
    public partial bool IsRecalculating { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RecalculateCommand))]
    [NotifyPropertyChangedFor(nameof(HasStats))]
    public partial bool CanRecalculate { get; set; }

    [ObservableProperty]
    public partial bool HasNoAchievements { get; set; }

    public bool HasStats => Sections.Count > 0;

    /// <param name="userId">Another member's id (admins only); null for your own.</param>
    public void Initialize(string? userId) => _requestedUserId = userId;

    [RelayCommand]
    public async Task LoadAsync()
    {
        HasLoadError = false;
        IsLoading = true;
        try
        {
            var me = currentUser.User ?? await currentUser.RefreshAsync();
            var isAdmin = me.IsInRole(RoleNames.Admin);
            // Like the SPA: only admins can open someone else's statistics.
            _userId = isAdmin && !string.IsNullOrEmpty(_requestedUserId) ? _requestedUserId : me.Id;
            var isOwn = _userId == me.Id;

            var statsTask = api.GetStatsAsync(_userId);
            var achievementsTask = TryAsync(() => api.GetAchievementEntriesAsync(_userId), (IReadOnlyList<AchievementEntry>)[]);
            var nameTask = isOwn ? Task.FromResult<string?>(null) : TryAsync(async () => (string?)(await api.GetUserProfileAsync(_userId)).DisplayName, null);
            var stats = await statsTask;
            var achievements = await achievementsTask;
            var name = await nameTask;

            Title = isOwn ? Texts.MyStats : Texts.StatsOf(name ?? "?");
            Subtitle = isOwn ? Texts.MyStatsSubtitle : Texts.StatsSubtitle;
            Achievements.Clear();
            foreach (var entry in achievements.OrderByDescending(a => a.DateAdded))
            {
                Achievements.Add(new AchievementChip(entry.AchievementId, entry.AchievementName, endpoints.ResolveApiUrl(entry.ImageUrl), entry.Tier));
            }
            HasNoAchievements = Achievements.Count == 0;
            Apply(stats, isAdmin);
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading statistics failed");
            HasLoadError = await LoadFailure.ReportAsync(dialogs, null, HasStats);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    private bool CanRecalculateNow() => CanRecalculate && !IsRecalculating;

    [RelayCommand(CanExecute = nameof(CanRecalculateNow))]
    private async Task RecalculateAsync()
    {
        if (_userId is null)
        {
            return;
        }
        IsRecalculating = true;
        try
        {
            var stats = await api.RecalculateStatsAsync(_userId);
            Apply(stats, currentUser.User?.IsInRole(RoleNames.Admin) ?? false);
            await dialogs.ToastAsync(Texts.StatsRecalculated);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            CanRecalculate = false;
            await dialogs.AlertAsync(Texts.Error, Texts.StatsRecalculateTooSoon);
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Recalculating statistics failed");
            await dialogs.AlertAsync(Texts.Error, Texts.StatsRecalculateFailed);
        }
        finally
        {
            IsRecalculating = false;
        }
    }

    [RelayCommand]
    private Task OpenAchievementAsync(AchievementChip chip) => navigator.OpenAsync($"achievements/{chip.AchievementId}");

    [RelayCommand]
    private Task OpenAllAchievementsAsync() =>
        navigator.OpenAsync(_userId is not null && _userId != currentUser.User?.Id ? $"achievements?userId={Uri.EscapeDataString(_userId)}" : "achievements");

    private void Apply(UserStats stats, bool isAdmin)
    {
        CanRecalculate = isAdmin || stats.LastStatsRecalculatedAt is not { } last || Display.IsUnset(last) ||
                         time.GetUtcNow().UtcDateTime - AsUtc(last) >= RecalculateInterval;
        LastRecalculatedText = Display.IsUnset(stats.LastStatsRecalculatedAt)
            ? null
            : Texts.LastRecalculated(Display.DateTimeMedium(stats.LastStatsRecalculatedAt!.Value));

        Highlights.Clear();
        Highlights.Add(new StatTile(Texts.TotalOrders, Display.Number(stats.TotalOrders), Icons.List));
        Highlights.Add(new StatTile(Texts.Spent, Display.Money(stats.TotalSpent), Icons.Wallet));
        Highlights.Add(new StatTile(Texts.Streak, Texts.Days(stats.CurrentStreak), Icons.Flame));
        Highlights.Add(new StatTile(Texts.Achievements, Display.Number(Achievements.Count), Icons.Trophy));

        Sections.Clear();
        Sections.Add(new StatSection(Texts.SectionOrdersAndSaldo, Icons.Wallet,
        [
            new(Texts.StatTotalOrders, Display.Number(stats.TotalOrders)),
            new(Texts.StatItemsBought, Display.Number(stats.TotalItemsBought)),
            new(Texts.StatTotalSpent, Display.Money(stats.TotalSpent)),
            new(Texts.StatTotalTopUp, Display.Money(stats.TotalTopUp)),
            new(Texts.StatMaxSingleTopUp, Display.Money(stats.MaxSingleTopUp)),
            new(Texts.StatMaxOrdersPerHour, Display.Number(stats.MaxOrdersPerHour)),
        ]));
        Sections.Add(new StatSection(Texts.SectionCommunity, Icons.Users,
        [
            new(Texts.StatQuotes, Display.Number(stats.QuoteCount)),
            new(Texts.StatQuoteVotesGiven, Display.Number(stats.QuoteVotesGiven)),
            new(Texts.StatQuoteVotesReceived, Display.Number(stats.QuoteVotesReceived)),
            new(Texts.StatReactions, Display.Number(stats.ReactionCount)),
            new(Texts.CurrentStreak, Texts.Days(stats.CurrentStreak)),
            new(Texts.StatMemberSince, Display.IsUnset(stats.MembershipStartDate) ? Texts.NoData : Display.Date(stats.MembershipStartDate!.Value)),
        ]));
        Sections.Add(new StatSection(Texts.SectionRecords, Icons.History,
        [
            new(Texts.StatMinBetweenOrders, MinutesText(stats.MinMinutesBetweenOrders)),
            new(Texts.StatMinBetweenTopUps, MinutesText(stats.MinMinutesBetweenTopUp)),
            new(Texts.StatLastActivity, WhenText(stats.LastActivityDate)),
            new(Texts.StatLastOrder, WhenText(stats.LastOrderDate)),
            new(Texts.StatLastTopUp, WhenText(stats.LastTopUpDate)),
        ]));
        OnPropertyChanged(nameof(HasStats));
    }

    private static string MinutesText(int minutes) => minutes == UserStats.NoMinutes ? Texts.NoData : Texts.Minutes(minutes);

    private static string WhenText(DateTime? value) => Display.IsUnset(value) ? Texts.NotHappenedYet : Display.DateTimeMedium(value!.Value);

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();

    private async Task<T> TryAsync<T>(Func<Task<T>> call, T fallback)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading part of the statistics failed");
            return fallback;
        }
    }

    private static bool IsExpected(Exception ex) =>
        ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException;
}
