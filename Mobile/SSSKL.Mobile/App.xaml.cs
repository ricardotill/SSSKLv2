using System.Globalization;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Popups;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Services;

namespace SSSKL.Mobile;

public partial class App : Application
{
    private readonly AppNavigator _navigator;
    private readonly AuthSession _session;
    private readonly AchievementPopupCoordinator _achievements;
    private readonly NotificationCenter _notifications;

    public App(AppNavigator navigator, AuthSession session, AchievementPopupCoordinator achievements, NotificationCenter notifications, ThemePreference theme)
    {
        // Dutch UI: prices as "€ 1,50" in bindings with StringFormat.
        var dutch = CultureInfo.GetCultureInfo("nl-NL");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = dutch;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = dutch;

        InitializeComponent();
        // The colour mode picked in the settings, before the first page is created.
        UserAppTheme = ThemeApplier.ToAppTheme(theme.Mode);
        _navigator = navigator;
        _session = session;
        _achievements = achievements;
        _notifications = notifications;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // The splash screen's colour (the logo's background), until the first real page is set.
        var window = new Window(new ContentPage { BackgroundColor = Color.FromArgb("#212529") }) { Title = "SSSKL" };
        window.Created += async (_, _) => await _navigator.StartAsync(window);
        window.Resumed += async (_, _) =>
        {
            if (await _session.IsSignedInAsync())
            {
                // Notifications may have arrived (as push messages) while the app was in the background.
                await Task.WhenAll(_achievements.CheckOnResumeAsync(), _notifications.RefreshAsync());
            }
        };
        return window;
    }
}
