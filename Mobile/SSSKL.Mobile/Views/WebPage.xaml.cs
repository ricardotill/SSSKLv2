using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.Web;
using SSSKL.Mobile.Services;

namespace SSSKL.Mobile.Views;

/// <summary>
/// Shows SPA features (events, leaderboard, admin, ...) in embedded mode, signed in with the native session.
/// <see cref="AppNavigator"/> keeps one instance per shell and pushes it on the "Meer" tab. After the first
/// load, pages are opened through the SPA's bridge (<c>window.ssskl.navigate</c>), which routes inside the
/// running app instead of reloading it. The WebView stays hidden until the SPA has rendered the page, then
/// fades in, so the previous page or a half-built one never flashes. A theme switch reloads it. ssskl-app:// navigations are handed
/// to <see cref="AppNavigator"/>.
/// </summary>
public partial class WebPage : ContentPage
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(40);
    private static readonly TimeSpan RouteTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan LoadTimeout = TimeSpan.FromSeconds(8);

    private readonly AppNavigator _navigator;
    private readonly WebNavigationPolicy _policy;
    private readonly AppEndpoints _endpoints;
    private readonly ThemePreference _theme;
    private readonly ILogger<WebPage> _logger;

    /// <summary>The SPA document has loaded, so the bridge can take navigations.</summary>
    private bool _loaded;
    private string _path = "";
    /// <summary>Bumped for every page shown; an older reveal stops when it sees a newer one.</summary>
    private int _showId;
    private bool _reloadPending;

    public WebPage(AppNavigator navigator, WebNavigationPolicy policy, AppEndpoints endpoints, ThemePreference theme, ILogger<WebPage> logger)
    {
        InitializeComponent();
        _navigator = navigator;
        _policy = policy;
        _endpoints = endpoints;
        _theme = theme;
        _logger = logger;
        _theme.Changed += OnThemeChanged;
        if (Application.Current is { } app)
        {
            app.RequestedThemeChanged += OnRequestedThemeChanged;
        }
        // The nav bar's back button goes back inside the SPA first, like Android's back button.
        Shell.SetBackButtonBehavior(this, new BackButtonBehavior { Command = new Command(() => _ = GoBackAsync()) });
    }

    /// <summary>Opens an SPA route. Call it before pushing the page, so the push never shows the previous page.</summary>
    public async Task ShowAsync(string path)
    {
        var showId = ++_showId;
        _path = path;
        ErrorPanel.IsVisible = false;
        Loading.IsVisible = true;
        WebView.CancelAnimations();
        WebView.Opacity = 0;

        if (_loaded && await TryEvaluateAsync(WebSessionScript.NavigateScript(path)) is { } result && WebSessionScript.IsTrue(result))
        {
            await RevealWhenRenderedAsync(showId, RouteTimeout);
            return;
        }
        Load(path);
    }

    /// <summary>Called when the shell is thrown away (sign-out): stop listening and free the WebView.</summary>
    public void Release()
    {
        _theme.Changed -= OnThemeChanged;
        if (Application.Current is { } app)
        {
            app.RequestedThemeChanged -= OnRequestedThemeChanged;
        }
        _showId++;
        this.DisconnectHandlers();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = GoBackAsync();
        return true;
    }

    private void Load(string path)
    {
        _loaded = false;
        WebView.Source = new UrlWebViewSource { Url = _endpoints.WebUrl(path).ToString() };
    }

    private async Task GoBackAsync()
    {
        if (_loaded && WebSessionScript.IsTrue(await TryEvaluateAsync(WebSessionScript.BackScript)))
        {
            return;
        }
        if (Navigation.NavigationStack.LastOrDefault() == this)
        {
            await Navigation.PopAsync();
        }
    }

    /// <summary>Polls the bridge until the SPA has rendered the page (or gives up), then fades the WebView in.</summary>
    private async Task RevealWhenRenderedAsync(int showId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline && showId == _showId &&
               WebSessionScript.IsTrue(await TryEvaluateAsync(WebSessionScript.BusyScript) ?? "true"))
        {
            await Task.Delay(PollInterval);
        }
        if (showId != _showId)
        {
            return;
        }
        Loading.IsVisible = false;
        await WebView.FadeToAsync(1, 180, Easing.CubicOut);
    }

    private async Task<string?> TryEvaluateAsync(string script)
    {
        try
        {
            return await WebView.EvaluateJavaScriptAsync(script);
        }
        catch (Exception ex)
        {
            // No handler yet, or the page is reloading: the caller falls back.
            _logger.LogDebug(ex, "Evaluating script in the WebView failed");
            return null;
        }
    }

    private void OnThemeChanged(object? sender, ThemeMode mode) => ScheduleReload();

    // The OS switched between light and dark (matters for "Auto"), or UserAppTheme changed.
    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e) => ScheduleReload();

    /// <summary>
    /// Reloads the SPA after a theme switch, so every part of it (PrimeNG, Tailwind, the status bar colour)
    /// renders in the new mode. A settings change raises both events, so they coalesce into one reload.
    /// The reload is posted twice: the platform code queues the WebSession script update (which carries the
    /// new mode) from its own Changed handler, which may run after this page's, so one hop could beat it.
    /// </summary>
    private void ScheduleReload()
    {
        if (_reloadPending)
        {
            return;
        }
        _reloadPending = true;
        MainThread.BeginInvokeOnMainThread(() => MainThread.BeginInvokeOnMainThread(() =>
        {
            _reloadPending = false;
            if (_path.Length == 0 || ErrorPanel.IsVisible)
            {
                return;
            }
            _showId++;
            Loading.IsVisible = true;
            WebView.CancelAnimations();
            WebView.Opacity = 0;
            if (_loaded)
            {
                // Reload, not Load(_path): the user may have navigated inside the SPA since.
                _loaded = false;
                WebView.Reload();
            }
            else
            {
                Load(_path);
            }
        }));
    }

    private async void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        var decision = _policy.Decide(e.Url);
        switch (decision.Kind)
        {
            case WebNavigationKind.Native:
                e.Cancel = true;
                await _navigator.HandleNativeActionAsync(decision.NativeAction!, decision.NativePath);
                break;
            case WebNavigationKind.External:
                e.Cancel = true;
                await Launcher.Default.TryOpenAsync(e.Url);
                break;
            case WebNavigationKind.Block:
                e.Cancel = true;
                break;
        }
    }

    private async void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success)
        {
            _loaded = false;
            Loading.IsVisible = false;
            ErrorPanel.IsVisible = true;
            return;
        }
        _loaded = true;
        await RevealWhenRenderedAsync(_showId, LoadTimeout);
    }

    private void OnRetry(object? sender, EventArgs e)
    {
        ErrorPanel.IsVisible = false;
        Loading.IsVisible = true;
        Load(_path);
    }
}
