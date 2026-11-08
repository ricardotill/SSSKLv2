using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Auth;
using SSSKL.Mobile.Core.Localization;
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
    IUserDialogs dialogs,
    ILogger<AppNavigator> logger) : IAppNavigator
{
    private Window? _window;
    private WebPage? _webPage;
    private string? _pendingWebPath;
    private bool _started;
    private bool _signingIn;

    /// <summary>
    /// The "Meer" tab registers itself when the shell creates it (a new one per shell, so per session) and
    /// gets the path to open first: the one <see cref="OpenWebAsync"/> asked for, or the start page.
    /// </summary>
    internal string Attach(WebPage page)
    {
        _webPage = page;
        var path = _pendingWebPath ?? WebPage.StartPath;
        _pendingWebPath = null;
        return path;
    }

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

    public async Task OpenWebAsync(string path)
    {
        var page = _webPage;
        if (page is null)
        {
            // The tab is created lazily on its first visit; the new page opens this path itself.
            _pendingWebPath = path;
        }
        await Shell.Current.GoToAsync("//more");
        page?.NavigateTo(path);
    }

    /// <summary>Handles <c>ssskl-app://&lt;action&gt;</c> navigations from the embedded SPA.</summary>
    public Task HandleNativeActionAsync(string action) => action switch
    {
        "pos" => Shell.Current.GoToAsync("//pos"),
        "profile" => Shell.Current.GoToAsync("//profile"),
        "logout" => SignOutAsync(),
        _ => Task.CompletedTask
    };

    private async Task ResetAsync()
    {
        await session.SignOutAsync();
        currentUser.Clear();
        webSession.Update(null);
        await webDataCleaner.ClearAsync();
        DetachWebPage();
        ShowLogin();
    }

    /// <summary>Signed out: the welcome page, which opens the SPA login as a sheet.</summary>
    private void ShowLogin() => SetRoot(services.GetRequiredService<WelcomePage>());

    private void DetachWebPage()
    {
        _webPage = null;
        _pendingWebPath = null;
    }

    private void ShowMain()
    {
        // A new shell creates its own "Meer" tab, which attaches itself.
        DetachWebPage();
        SetRoot(services.GetRequiredService<AppShell>());
        _ = RunStartupChecksAsync();
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
