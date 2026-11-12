using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// Leaderboard (SPA leaderboard): who bought the most of a product in the last 12 hours, this month or
/// ever. The SPA shows the three periods below each other; the app switches between them.
/// </summary>
public sealed partial class LeaderboardViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    ILogger<LeaderboardViewModel> logger) : ObservableObject
{
    /// <summary>Ignores selection changes while the products are (re)loaded.</summary>
    private bool _loadingProducts;

    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<MemberRow> Entries { get; } = [];

    [ObservableProperty]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Is12Hours), nameof(IsMonth), nameof(IsAllTime))]
    public partial LeaderboardPeriod Period { get; set; } = LeaderboardPeriod.Last12Hours;

    public bool Is12Hours => Period == LeaderboardPeriod.Last12Hours;
    public bool IsMonth => Period == LeaderboardPeriod.Month;
    public bool IsAllTime => Period == LeaderboardPeriod.AllTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(ShowSkeleton))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(ShowSkeleton))]
    public partial bool HasLoaded { get; set; }

    /// <summary>The first load failed: the page shows its error state with a retry button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial bool HasLoadError { get; set; }

    /// <summary>Placeholders while the first load runs (not while refreshing loaded data).</summary>
    public bool ShowSkeleton => IsLoading && !HasLoaded && !HasLoadError;

    public bool IsEmpty => HasLoaded && !IsLoading && Entries.Count == 0;

    /// <summary>Loads the products (those with a leaderboard) and the ranking of the selected one.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        HasLoadError = false;
        try
        {
            _loadingProducts = true;
            if (!HasLoaded)
            {
                IsLoading = true; // the skeleton covers loading the products too
            }
            var products = await api.GetProductsAsync();
            var withLeaderboard = products.Where(p => p.EnableLeaderboard).ToList();
            var previous = SelectedProduct?.Id;
            Products.Clear();
            foreach (var product in withLeaderboard.Count > 0 ? withLeaderboard : products)
            {
                Products.Add(product);
            }
            SelectedProduct = Products.FirstOrDefault(p => p.Id == previous) ?? Products.FirstOrDefault();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading products failed");
            IsRefreshing = false;
            IsLoading = false;
            HasLoadError = await LoadFailure.ReportAsync(dialogs, null, HasLoaded);
            return;
        }
        finally
        {
            _loadingProducts = false;
        }
        await LoadEntriesAsync();
    }

    [RelayCommand]
    private Task SetPeriodAsync(LeaderboardPeriod period)
    {
        if (period == Period)
        {
            return Task.CompletedTask;
        }
        Period = period;
        return LoadEntriesAsync();
    }

    [RelayCommand]
    private Task OpenAsync(MemberRow member) =>
        member.UserId is null ? Task.CompletedTask : navigator.OpenAsync(NativeRoutes.UserProfilePath(member.UserId));

    partial void OnSelectedProductChanged(Product? value)
    {
        if (!_loadingProducts && value is not null)
        {
            _ = LoadEntriesAsync();
        }
    }

    private async Task LoadEntriesAsync()
    {
        if (SelectedProduct is not { } product)
        {
            IsLoading = false;
            IsRefreshing = false;
            HasLoaded = true;
            Entries.Clear();
            OnPropertyChanged(nameof(IsEmpty));
            return;
        }
        IsLoading = true;
        try
        {
            var period = Period;
            var entries = await api.GetLeaderboardAsync(product.Id, period);
            // A newer choice was made meanwhile; its own load shows the result.
            if (period != Period || product != SelectedProduct)
            {
                return;
            }
            var me = currentUser.User?.Id;
            Entries.Clear();
            foreach (var entry in entries.OrderBy(e => e.Position))
            {
                Entries.Add(new MemberRow(entry.UserId, entry.FullName, endpoints.ResolveApiUrl(entry.ProfilePictureUrl),
                    Display.Number(entry.Amount), IsSelf: entry.UserId is not null && entry.UserId == me, Rank: entry.Position));
            }
            HasLoaded = true;
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading the leaderboard failed");
            HasLoadError = await LoadFailure.ReportAsync(dialogs, null, HasLoaded);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private static bool IsExpected(Exception ex) =>
        ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException;
}
