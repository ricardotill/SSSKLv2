using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.Popups;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Views;

namespace SSSKL.Mobile.Services;

/// <summary>
/// Switches the window between the login WebView and the tab shell, and carries out navigation that
/// view models and the embedded SPA (ssskl-app:// actions) ask for.
/// </summary>
public sealed class AppNavigator(
    IServiceProvider services,
    AuthSession session,
    CurrentUserService currentUser,
    IWebDataCleaner webDataCleaner,
    WebSession webSession,
    AchievementPopupCoordinator achievements,
    WhatsNewCoordinator whatsNew,
    NotificationCenter notifications,
    IUserDialogs dialogs,
    ILogger<AppNavigator> logger) : IAppNavigator
{
    /// <summary>The "Meer" tab's route (AppShell.xaml); web pages are pushed on its stack.</summary>
    private const string MoreRoute = "more";

    private Window? _window;
    /// <summary>The WebView page of the current shell, created on first use and reused (see <see cref="WebPage"/>).</summary>
    private WebPage? _webPage;
    /// <summary>The current tab shell; released when it is replaced (sign-out, new sign-in).</summary>
    private AppShell? _shell;
    private bool _started;
    private bool _signingIn;
    /// <summary>Drops taps that arrive while a page is being pushed (a double tap would push it twice).</summary>
    private bool _navigating;

    public async Task StartAsync(Window window)
    {
        _window = window;
        if (!_started)
        {
            _started = true;
            session.SessionExpired += (_, _) => MainThread.BeginInvokeOnMainThread(async () =>
            {
                await ResetAsync();
                await dialogs.AlertAsync(Texts.AppName, Texts.SessionExpired);
            });
        }

        var tokens = await session.GetTokensAsync();
        webSession.Update(tokens);
        if (tokens is null)
        {
            ShowLogin();
        }
        else
        {
            ShowMain();
        }
    }

    /// <summary>Called by the login sheet (after dismissing itself) with the tokens the SPA obtained.</summary>
    public async Task CompleteSignInAsync(TokenSet tokens)
    {
        if (_signingIn)
        {
            return;
        }
        _signingIn = true;
        try
        {
            await session.SignInAsync(tokens);
            ShowMain();
        }
        finally
        {
            _signingIn = false;
        }
    }

    public async Task SignOutAsync()
    {
        await ResetAsync();
    }

    public Task OpenAsync(string path) => NativeRoutes.Resolve(path) switch
    {
        null => OpenWebAsync(path.TrimStart('/')),
        { Page: NativePage.Pos } => SwitchTabAsync("pos"),
        { Page: NativePage.Notifications } => SwitchTabAsync("notifications"),
        { Page: NativePage.Profile } => OpenProfileAsync(),
        { Page: NativePage.Settings } => OpenSettingsAsync(),
        // Your own profile is the Profiel tab.
        { Page: NativePage.UserProfile, Id: var id } when id == currentUser.User?.Id => OpenProfileAsync(),
        { Page: NativePage.Orders } route => PushAsync<OrdersPage>(route),
        { Page: NativePage.TopUps } route => PushAsync<TopUpsPage>(route),
        { Page: NativePage.SaldoList } route => PushAsync<SaldoListPage>(route),
        { Page: NativePage.UserProfile } route => PushAsync<UserProfilePage>(route),
        { Page: NativePage.Leaderboard } route => PushAsync<LeaderboardPage>(route),
        { Page: NativePage.Stats } route => PushAsync<StatsPage>(route),
        { Page: NativePage.Achievements } route => PushAsync<AchievementsPage>(route),
        { Page: NativePage.AchievementDetail } route => PushAsync<AchievementDetailPage>(route),
        { Page: NativePage.About } route => PushAsync<AboutPage>(route),
        _ => OpenWebAsync(path.TrimStart('/'))
    };

    /// <summary>Pushes a native page on the current tab (back returns to where you were).</summary>
    private Task PushAsync<TPage>(NativeRoute route) where TPage : Page => GuardedAsync(async shell =>
    {
        var page = services.GetRequiredService<TPage>();
        (page as INativePage)?.Prepare(route);
        await shell.Navigation.PushAsync(page);
    });

    /// <summary>
    /// Shows an SPA route on the "Meer" tab: on top of what that tab shows (so back returns there), or in
    /// place when the web page is already on top.
    /// </summary>
    public Task OpenWebAsync(string path) => GuardedAsync(async shell =>
    {
        var page = _webPage ??= services.GetRequiredService<WebPage>();
        if (shell.CurrentItem?.CurrentItem?.Route != MoreRoute)
        {
            await shell.GoToAsync("//" + MoreRoute);
        }

        var stack = shell.Navigation.NavigationStack;
        if (stack.LastOrDefault() == page)
        {
            Forget(page.ShowAsync(path));
            return;
        }
        if (stack.Contains(page))
        {
            await shell.Navigation.PopToRootAsync(false);
        }
        // Start (and hide the old page) before the push animation; the page fades in once rendered. Not
        // awaited here: the guard must not block other taps while the SPA renders.
        Forget(page.ShowAsync(path));
        await shell.Navigation.PushAsync(page);
    });

    public Task OpenSettingsAsync() => GuardedAsync(async shell =>
    {
        if (shell.Navigation.NavigationStack.LastOrDefault() is SettingsPage)
        {
            return;
        }
        await shell.Navigation.PushAsync(services.GetRequiredService<SettingsPage>());
    });

    public Task OpenProfileAsync() => SwitchTabAsync("profile");

    /// <summary>Switches to a tab (a route in AppShell.xaml).</summary>
    private Task SwitchTabAsync(string route) => GuardedAsync(shell => shell.GoToAsync("//" + route));

    /// <summary>Handles <c>ssskl-app://&lt;action&gt;</c> navigations from the embedded SPA.</summary>
    public Task HandleNativeActionAsync(string action) => action switch
    {
        "pos" => SwitchTabAsync("pos"),
        "profile" => OpenProfileAsync(),
        "settings" => OpenSettingsAsync(),
        "logout" => SignOutAsync(),
        _ => Task.CompletedTask
    };

    private void Forget(Task task) =>
        task.ContinueWith(t => logger.LogWarning(t.Exception, "Showing a web page failed"),
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

    private async Task GuardedAsync(Func<Shell, Task> navigate)
    {
        if (_navigating || Shell.Current is not { } shell)
        {
            return;
        }
        _navigating = true;
        try
        {
            await navigate(shell);
        }
        catch (Exception ex)
        {
            // A navigation that fails (e.g. the shell was replaced meanwhile) must not crash the app.
            logger.LogWarning(ex, "Navigation failed");
        }
        finally
        {
            _navigating = false;
        }
    }

    private async Task ResetAsync()
    {
        await session.SignOutAsync();
        currentUser.Clear();
        notifications.Clear();
        webSession.Update(null);
        await webDataCleaner.ClearAsync();
        DetachWebPage();
        ReleaseShell();
        ShowLogin();
    }

    /// <summary>Signed out: the welcome page, which opens the SPA login as a sheet.</summary>
    private void ShowLogin() => SetRoot(services.GetRequiredService<WelcomePage>());

    /// <summary>Frees the web page of the previous shell, so no SPA state of a previous user survives.</summary>
    private void DetachWebPage()
    {
        var page = _webPage;
        _webPage = null;
        if (page is not null)
        {
            MainThread.BeginInvokeOnMainThread(page.Release);
        }
    }

    private void ShowMain()
    {
        // A new shell gets a new web page on first use.
        DetachWebPage();
        ReleaseShell();
        _shell = services.GetRequiredService<AppShell>();
        SetRoot(_shell);
        _ = RunStartupChecksAsync();
    }

    /// <summary>Stops the previous shell listening to the singletons, so it can be collected.</summary>
    private void ReleaseShell()
    {
        var shell = _shell;
        _shell = null;
        if (shell is not null)
        {
            MainThread.BeginInvokeOnMainThread(shell.Release);
        }
    }

    private void SetRoot(Page page)
    {
        if (_window is not null)
        {
            MainThread.BeginInvokeOnMainThread(() => _window.Page = page);
        }
    }

    /// <summary>"What's new" first, then new achievements; popups are shown one after another.</summary>
    private async Task RunStartupChecksAsync()
    {
        try
        {
            await currentUser.RefreshAsync();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Loading the current user failed");
        }
        // Logs its own failures.
        await notifications.RefreshAsync();
        // The coordinators log expected failures themselves; anything else must not skip the achievement
        // check (unseen achievements are only shown from here and on resume).
        try
        {
            await whatsNew.CheckAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Checking what's new failed");
        }
        try
        {
            await achievements.CheckAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Checking unseen achievements failed");
        }
    }
}
