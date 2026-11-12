using System.ComponentModel;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Views;

namespace SSSKL.Mobile;

public partial class AppShell : Shell
{
    private readonly NotificationInbox _notifications;

    public AppShell(IServiceProvider services, NotificationInbox notifications)
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
        UpdateTabIcons();
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        UpdateTabIcons();
    }

    /// <summary>The selected tab shows its filled icon, the others their outline.</summary>
    private void UpdateTabIcons()
    {
        var selected = CurrentItem?.CurrentItem;
        foreach (var (tab, name) in new[] { (PosTab, "pos"), (NotificationsTab, "notifications"), (ProfileTab, "profile"), (MoreTab, "more") })
        {
            var file = tab == selected ? $"tab_{name}_selected.png" : $"tab_{name}.png";
            if (tab.Icon is not FileImageSource current || current.File != file)
            {
                tab.Icon = file;
            }
        }
    }

    /// <summary>Called by <see cref="Services.AppNavigator"/> when this shell is replaced.</summary>
    public void Release() => _notifications.PropertyChanged -= OnNotificationsChanged;

    // NotificationInbox may change on any thread; the tab bar must be touched on the main thread.
    private void OnNotificationsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotificationInbox.BadgeText))
        {
            MainThread.BeginInvokeOnMainThread(UpdateBadge);
        }
    }

    private void UpdateBadge() => NotificationsTab.BadgeText = _notifications.BadgeText;
}
