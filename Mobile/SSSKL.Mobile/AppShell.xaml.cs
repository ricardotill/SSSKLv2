using System.ComponentModel;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Views;

namespace SSSKL.Mobile;

public partial class AppShell : Shell
{
    private readonly NotificationCenter _notifications;

    public AppShell(IServiceProvider services, NotificationCenter notifications)
    {
        InitializeComponent();
        // Pages come from DI; DataTemplates resolve them lazily on first visit.
        PosContent.ContentTemplate = new DataTemplate(services.GetRequiredService<PosPage>);
        NotificationsContent.ContentTemplate = new DataTemplate(services.GetRequiredService<NotificationsPage>);
        ProfileContent.ContentTemplate = new DataTemplate(services.GetRequiredService<ProfilePage>);
        MoreContent.ContentTemplate = new DataTemplate(services.GetRequiredService<MorePage>);

        _notifications = notifications;
        _notifications.PropertyChanged += OnNotificationsChanged;
        UpdateBadge();
    }

    /// <summary>Called by <see cref="Services.AppNavigator"/> when this shell is replaced.</summary>
    public void Release() => _notifications.PropertyChanged -= OnNotificationsChanged;

    // NotificationCenter may change on any thread; the tab bar must be touched on the main thread.
    private void OnNotificationsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotificationCenter.BadgeText))
        {
            MainThread.BeginInvokeOnMainThread(UpdateBadge);
        }
    }

    private void UpdateBadge() => NotificationsTab.BadgeText = _notifications.BadgeText;
}
