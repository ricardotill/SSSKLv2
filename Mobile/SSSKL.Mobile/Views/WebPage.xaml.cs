using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Web;
using SSSKL.Mobile.Services;

namespace SSSKL.Mobile.Views;

/// <summary>
/// The "Meer" tab: the full SPA (events, leaderboard, settings, admin, ...) in embedded mode, signed in
/// with the native session. ssskl-app:// navigations are handed to <see cref="AppNavigator"/>.
/// </summary>
public partial class WebPage : ContentPage
{
    public const string StartPath = "events";

    private readonly AppNavigator _navigator;
    private readonly WebNavigationPolicy _policy;
    private readonly AppEndpoints _endpoints;

    public WebPage(AppNavigator navigator, WebNavigationPolicy policy, AppEndpoints endpoints)
    {
        InitializeComponent();
        _navigator = navigator;
        _policy = policy;
        _endpoints = endpoints;
        navigator.Attach(this);
        NavigateTo(StartPath);
    }

    public void NavigateTo(string path)
    {
        Loading.IsVisible = true;
        WebView.Source = new UrlWebViewSource { Url = _endpoints.WebUrl(path).ToString() };
    }

    protected override bool OnBackButtonPressed()
    {
        if (WebView.CanGoBack)
        {
            WebView.GoBack();
            return true;
        }
        return base.OnBackButtonPressed();
    }

    private async void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        var decision = _policy.Decide(e.Url);
        switch (decision.Kind)
        {
            case WebNavigationKind.Native:
                e.Cancel = true;
                await _navigator.HandleNativeActionAsync(decision.NativeAction!);
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

    private void OnNavigated(object? sender, WebNavigatedEventArgs e) => Loading.IsVisible = false;
}
