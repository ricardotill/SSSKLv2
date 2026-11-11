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

public enum AchievementFilter
{
    All,
    Earned,
    Locked
}

/// <summary>An achievement card: earned ones in colour with their date, locked ones greyed out.</summary>
public sealed record AchievementRow(
    Guid Id, string Name, string Description, string? ImageUrl, string Tier, bool Completed, string StatusText, string? ParentText)
{
    public static AchievementRow From(AchievementListing listing, string? imageUrl) => new(
        listing.Id, listing.Name, listing.Description, imageUrl, listing.Tier, listing.Completed,
        listing.Completed && listing.DateAdded is { } date ? Texts.AchievedOn(Display.Date(date)) : Texts.NotAchievedYet,
        listing.ParentAchievementName is { Length: > 0 } parent ? Texts.PartOf(parent) : null);

    public bool HasImage => ImageUrl is not null;
    public bool HasNoImage => ImageUrl is null;
    public bool HasParent => ParentText is not null;
    public bool IsLocked => !Completed;
    public string AccessibleText => $"{Name}. {Description}. {StatusText}";
}

/// <summary>
/// Achievements (SPA achievements): every achievement with your progress (or another member's, from their
/// profile: <c>?userId=</c>), earned first and newest first, filterable on earned/not yet.
/// </summary>
public sealed partial class AchievementsViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    ILogger<AchievementsViewModel> logger) : ObservableObject
{
    private string? _requestedUserId;
    private List<AchievementRow> _all = [];

    public ObservableCollection<AchievementRow> Items { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = Texts.Achievements;

    [ObservableProperty]
    public partial string Subtitle { get; set; } = Texts.AchievementsSubtitle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAll), nameof(IsEarned), nameof(IsLocked))]
    public partial AchievementFilter Filter { get; set; } = AchievementFilter.All;

    public bool IsAll => Filter == AchievementFilter.All;
    public bool IsEarned => Filter == AchievementFilter.Earned;
    public bool IsLocked => Filter == AchievementFilter.Locked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool HasLoaded { get; set; }

    public bool IsEmpty => HasLoaded && !IsLoading && Items.Count == 0;

    /// <param name="userId">Another member's id; null for your own.</param>
    public void Initialize(string? userId) => _requestedUserId = userId;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var me = currentUser.User ?? await currentUser.RefreshAsync();
            var userId = string.IsNullOrEmpty(_requestedUserId) ? me.Id : _requestedUserId;
            var isOwn = userId == me.Id;

            var listingTask = api.GetAchievementListingAsync(userId);
            var nameTask = isOwn ? Task.FromResult<string?>(null) : GetNameAsync(userId);
            var listing = await listingTask;
            var name = await nameTask;

            // Like the SPA: earned first (newest first), then by name.
            _all = listing
                .OrderByDescending(a => a.Completed)
                .ThenByDescending(a => a.Completed ? a.DateAdded ?? DateTime.MinValue : DateTime.MinValue)
                .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(a => AchievementRow.From(a, endpoints.ResolveApiUrl(a.ImageUrl)))
                .ToList();
            Title = isOwn ? Texts.Achievements : Texts.AchievementsOf(name ?? "?");
            Subtitle = Texts.EarnedCount(_all.Count(a => a.Completed), _all.Count);
            HasLoaded = true;
            ApplyFilter();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading achievements failed");
            await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    private void SetFilter(AchievementFilter filter)
    {
        Filter = filter;
        ApplyFilter();
    }

    [RelayCommand]
    private Task OpenAsync(AchievementRow row) => navigator.OpenAsync($"achievements/{row.Id}");

    private void ApplyFilter()
    {
        Items.Clear();
        foreach (var row in _all.Where(a => Filter switch
                 {
                     AchievementFilter.Earned => a.Completed,
                     AchievementFilter.Locked => !a.Completed,
                     _ => true
                 }))
        {
            Items.Add(row);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }

    private async Task<string?> GetNameAsync(string userId)
    {
        try
        {
            return (await api.GetUserProfileAsync(userId)).DisplayName;
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading the member's name failed");
            return null;
        }
    }

    private static bool IsExpected(Exception ex) =>
        ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException;
}
