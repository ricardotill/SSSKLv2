using System.Globalization;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Popups;
using SSSKL.Mobile.Services;

namespace SSSKL.Mobile;

public partial class App : Application
{
    private readonly AppNavigator _navigator;
    private readonly AuthSession _session;
    private readonly AchievementPopupCoordinator _achievements;

    public App(AppNavigator navigator, AuthSession session, AchievementPopupCoordinator achievements)
    {
        // Dutch UI: prices as "€ 1,50" in bindings with StringFormat.
        var dutch = CultureInfo.GetCultureInfo("nl-NL");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = dutch;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = dutch;

        InitializeComponent();
        _navigator = navigator;
        _session = session;
        _achievements = achievements;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new ContentPage { BackgroundColor = Color.FromArgb("#34D399") }) { Title = "SSSKL" };
        window.Created += async (_, _) => await _navigator.StartAsync(window);
        window.Resumed += async (_, _) =>
        {
            if (await _session.IsSignedInAsync())
            {
                await _achievements.CheckOnResumeAsync();
            }
        };
        return window;
    }
}
