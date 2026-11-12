using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// A list loaded a page at a time while scrolling (orders, top-ups): pull-to-refresh reloads the first
/// page, <see cref="LoadMoreCommand"/> appends the next one.
/// </summary>
public abstract partial class PagedListViewModel<TItem>(IUserDialogs dialogs, ILogger logger) : ObservableObject
{
    public const int PageSize = 20;

    public ObservableCollection<TItem> Items { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(ShowSkeleton))]
    public partial bool IsLoading { get; set; }

    /// <summary>The first load failed: the page shows its error state with a retry button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial bool HasLoadError { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    [ObservableProperty]
    public partial bool HasMore { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(ShowSkeleton))]
    public partial bool HasLoaded { get; set; }

    /// <summary>Placeholder rows while the first page loads (not while refreshing a loaded list).</summary>
    public bool ShowSkeleton => IsLoading && !HasLoaded && !HasLoadError;

    /// <summary>Shows the empty state; not before the first load finished.</summary>
    public bool IsEmpty => HasLoaded && !IsLoading && Items.Count == 0;

    protected IUserDialogs Dialogs => dialogs;
    protected ILogger Logger => logger;

    /// <summary>One page and the total count.</summary>
    protected abstract Task<(IReadOnlyList<TItem> Items, int Total)> FetchAsync(int skip, int take);

    /// <summary>Identity for skipping duplicates when pages shift (an item was added meanwhile).</summary>
    protected abstract object KeyOf(TItem item);

    [RelayCommand]
    public async Task LoadAsync()
    {
        HasLoadError = false;
        IsLoading = true;
        try
        {
            var (items, total) = await FetchAsync(0, PageSize);
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
            HasMore = Items.Count < total && items.Count == PageSize;
            HasLoaded = true;
            await OnLoadedAsync();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading {List} failed", GetType().Name);
            HasLoadError = await LoadFailure.ReportAsync(dialogs, null, HasLoaded);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (!HasMore || IsLoading || IsLoadingMore)
        {
            return;
        }
        IsLoadingMore = true;
        try
        {
            var (items, total) = await FetchAsync(Items.Count, PageSize);
            var shown = Items.Select(KeyOf).ToHashSet();
            foreach (var item in items.Where(i => shown.Add(KeyOf(i))))
            {
                Items.Add(item);
            }
            HasMore = Items.Count < total && items.Count == PageSize;
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            // Scrolling again retries; no alert while scrolling.
            logger.LogWarning(ex, "Loading more of {List} failed", GetType().Name);
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    /// <summary>After the first page loaded (e.g. to refresh a header).</summary>
    protected virtual Task OnLoadedAsync() => Task.CompletedTask;

    protected void NotifyEmptyChanged() => OnPropertyChanged(nameof(IsEmpty));

    protected static bool IsExpected(Exception ex) =>
        ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException;
}
