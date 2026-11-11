using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Navigation;

namespace SSSKL.Mobile.Core.Services;

/// <summary>
/// The unread notification count behind the Notificaties tab badge. Singleton. Refreshed at sign-in, on
/// resume and when the tab appears; marking notifications read updates it straight away.
/// <see cref="ObservableObject.PropertyChanged"/> may be raised on any thread.
/// </summary>
public sealed partial class NotificationCenter(ISssklApi api, ILogger<NotificationCenter> logger) : ObservableObject
{
    /// <summary>Bumped by every local change, so a refresh that started before it can't undo it.</summary>
    private int _version;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BadgeText))]
    public partial int UnreadCount { get; private set; }

    /// <summary>The tab badge: null hides it.</summary>
    public string? BadgeText => MoreMenu.BadgeText(UnreadCount);

    /// <summary>Loads the count. Failures are logged and keep the last known count.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var version = Volatile.Read(ref _version);
        try
        {
            var count = await api.GetUnreadNotificationCountAsync(cancellationToken);
            if (version == Volatile.Read(ref _version))
            {
                UnreadCount = Math.Max(0, count);
            }
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Loading the unread notification count failed");
        }
    }

    /// <summary>One notification was marked read.</summary>
    public void MarkedRead()
    {
        Interlocked.Increment(ref _version);
        UnreadCount = Math.Max(0, UnreadCount - 1);
    }

    /// <summary>All notifications were marked read, or the user signed out.</summary>
    public void Clear()
    {
        Interlocked.Increment(ref _version);
        UnreadCount = 0;
    }
}
