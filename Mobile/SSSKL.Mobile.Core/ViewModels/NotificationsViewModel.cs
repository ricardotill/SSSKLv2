using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// The Notificaties tab, like the SPA's /notifications page (features/notifications): read and unread
/// notifications, newest first, loaded 20 at a time while scrolling. Tapping one marks it read and opens
/// its link; "Alles gelezen" marks them all.
/// </summary>
public sealed partial class NotificationsViewModel(
    ISssklApi api,
    NotificationInbox center,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    ILogger<NotificationsViewModel> logger) : ObservableObject
{
    public const int PageSize = 20;

    public ObservableCollection<NotificationItemViewModel> Items { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingMore { get; set; }

    [ObservableProperty]
    public partial bool HasMore { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial bool HasLoaded { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MarkAllReadCommand))]
    public partial bool IsMarkingAll { get; set; }

    /// <summary>Shows the empty state; not before the first load finished.</summary>
    public bool IsEmpty => HasLoaded && !IsLoading && Items.Count == 0;

    /// <summary>Like the SPA: "Alles gelezen" only while something is unread.</summary>
    public bool CanMarkAll => Items.Any(i => !i.IsRead) || center.UnreadCount > 0;

    /// <summary>Loads (or reloads) the first page, and the badge count with it.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var countTask = center.RefreshAsync();
            var page = await api.GetNotificationsAsync(0, PageSize);
            Items.Clear();
            foreach (var item in page)
            {
                Items.Add(new NotificationItemViewModel(item));
            }
            HasMore = page.Count == PageSize;
            HasLoaded = true;
            await countTask;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Loading notifications failed");
            await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            NotifyListChanged();
        }
    }

    /// <summary>The next page, when the list is scrolled near its end.</summary>
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
            var page = await api.GetNotificationsAsync(Items.Count, PageSize);
            // New notifications shift the pages; skip what is already shown.
            var shown = Items.Select(i => i.Id).ToHashSet();
            foreach (var item in page.Where(i => shown.Add(i.Id)))
            {
                Items.Add(new NotificationItemViewModel(item));
            }
            HasMore = page.Count == PageSize;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            // Scrolling again retries; no alert while scrolling.
            logger.LogWarning(ex, "Loading more notifications failed");
        }
        finally
        {
            IsLoadingMore = false;
            NotifyListChanged();
        }
    }

    [RelayCommand]
    private async Task OpenAsync(NotificationItemViewModel item)
    {
        if (!item.IsRead)
        {
            // Optimistic, so the row and the badge change at once; undone when the call fails.
            item.IsRead = true;
            center.MarkedRead();
            NotifyListChanged();
            try
            {
                await api.MarkNotificationReadAsync(item.Id);
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "Marking notification {Id} read failed", item.Id);
                item.IsRead = false;
                await center.RefreshAsync();
                NotifyListChanged();
            }
        }

        if (item.LinkUri is not null)
        {
            await navigator.OpenAsync(item.LinkUri);
        }
    }

    private bool CanMarkAllRead() => !IsMarkingAll;

    [RelayCommand(CanExecute = nameof(CanMarkAllRead))]
    private async Task MarkAllReadAsync()
    {
        IsMarkingAll = true;
        try
        {
            await api.MarkAllNotificationsReadAsync();
            foreach (var item in Items)
            {
                item.IsRead = true;
            }
            center.Clear();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Marking all notifications read failed");
            await dialogs.AlertAsync(Texts.Error, Texts.MarkAllReadFailed);
        }
        finally
        {
            IsMarkingAll = false;
            NotifyListChanged();
        }
    }

    private void NotifyListChanged()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CanMarkAll));
    }
}
