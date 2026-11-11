using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Web;
using SSSKL.Mobile.Services;

namespace SSSKL.Mobile.Views;

/// <summary>
/// Signs in through the SPA's own /login page, so Turnstile, two-factor and passkeys keep working.
/// When the SPA reports it is signed in (it navigates to ssskl-app://pos after login, see the
/// embedded-mode guard), the tokens are read from its storage and handed to the native session.
/// A timer polls as a fallback, e.g. for an SPA version without embedded mode.
/// Shown as a closable sheet over <see cref="WelcomePage"/> (a page sheet on iOS, so it can also be
/// swiped down); a new instance is created for every attempt.
/// </summary>
public partial class LoginPage : ContentPage
{
    private readonly AppNavigator _navigator;
    private readonly WebNavigationPolicy _policy;
    private readonly AppEndpoints _endpoints;
    private readonly IDispatcherTimer _poll;
    private bool _completed;
    private bool _closing;

    public LoginPage(AppNavigator navigator, WebNavigationPolicy policy, AppEndpoints endpoints)
    {
        InitializeComponent();
        On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.PageSheet);
        _navigator = navigator;
        _policy = policy;
        _endpoints = endpoints;
        _poll = Dispatcher.CreateTimer();
        _poll.Interval = TimeSpan.FromSeconds(1.5);
        _poll.Tick += async (_, _) => await TryCompleteAsync();
        Load();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_closing && !_completed)
        {
            _poll.Start();
        }
    }

    protected override void OnDisappearing()
    {
        _poll.Stop();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();
        return true;
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await CloseAsync();

    /// <summary>Dismisses the sheet (back to the welcome page). Idempotent.</summary>
    private async Task CloseAsync()
    {
        if (_closing)
        {
            return;
        }
        _closing = true;
        _poll.Stop();
        if (!Navigation.ModalStack.Contains(this))
        {
            return; // Already gone, e.g. swiped down on iOS.
        }
        try
        {
            await Navigation.PopModalAsync(animated: true);
        }
        catch (InvalidOperationException)
        {
            // Dismissed concurrently (swipe-down during the close); nothing left to pop.
        }
    }

    private void Load()
    {
        ErrorPanel.IsVisible = false;
        Loading.IsVisible = true;
        WebView.Source = new UrlWebViewSource { Url = _endpoints.WebUrl("login?returnUrl=%2Fpos").ToString() };
    }

    private async void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        var decision = _policy.Decide(e.Url);
        switch (decision.Kind)
        {
            case WebNavigationKind.Native:
                e.Cancel = true;
                await TryCompleteAsync();
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

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        Loading.IsVisible = false;
        // Failures of sub-navigations we cancelled ourselves are reported too; only react to the SPA itself.
        if (e.Result == WebNavigationResult.Failure && _policy.Decide(e.Url).Kind == WebNavigationKind.Allow)
        {
            ErrorPanel.IsVisible = true;
        }
    }

    private void OnRetry(object? sender, EventArgs e) => Load();

    private async Task TryCompleteAsync()
    {
        if (_completed || _closing)
        {
            return;
        }
        string? result;
        try
        {
            result = await WebView.EvaluateJavaScriptAsync(WebSessionScript.ReadTokensScript);
        }
        catch (Exception)
        {
            return; // Page not ready yet.
        }

        var tokens = WebSessionScript.ParseTokens(result);
        if (tokens is null || _completed || _closing)
        {
            return;
        }
        _completed = true;
        // Dismiss the sheet first, then replace the window root with the app shell.
        await CloseAsync();
        await _navigator.CompleteSignInAsync(tokens);
    }
}
