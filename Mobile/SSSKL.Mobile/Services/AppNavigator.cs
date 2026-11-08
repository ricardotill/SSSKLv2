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
    private bool _started;

    /// <summary>The "Meer" tab registers itself when the shell creates it.</summary>
    internal void Attach(WebPage page) => _webPage = page;

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

    /// <summary>Called by the login page with the tokens the SPA obtained.</summary>
    public async Task CompleteSignInAsync(TokenSet tokens)
    {
        await session.SignInAsync(tokens);
        ShowMain();
    }

    public async Task SignOutAsync()
    {
        await ResetAsync();
    }

    public async Task OpenWebAsync(string path)
    {
        await Shell.Current.GoToAsync("//more");
        _webPage?.NavigateTo(path);
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
        _webPage = null;
        ShowLogin();
    }

    private void ShowLogin() => SetRoot(services.GetRequiredService<LoginPage>());

    private void ShowMain()
    {
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
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Loading the current user failed");
        }
        await whatsNew.CheckAsync();
        await achievements.CheckAsync();
    }
}
