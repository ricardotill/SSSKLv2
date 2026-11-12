using SSSKL.Mobile.Services;

namespace SSSKL.Mobile.Views;

/// <summary>
/// Shown while signed out: the logo on the brand colour with an "Inloggen" button that opens the SPA login
/// (<see cref="LoginPage"/>) as a closable sheet on top of this page.
/// </summary>
public partial class WelcomePage : ContentPage
{
    private static readonly Uri AuthorUrl = new("https://ricardotill.nl");

    private readonly IServiceProvider _services;
    private bool _opening;

    public WelcomePage(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        VersionLabel.Text = $"v{AppInfo.Current.VersionString}";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BrandSystemBars.Set(true);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BrandSystemBars.Set(false);
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        // A double tap must not stack a second login page.
        if (_opening)
        {
            return;
        }
        _opening = true;
        try
        {
            // A new page per tap: a sheet dismissed by swiping down leaves nothing behind to reuse.
            await Navigation.PushModalAsync(_services.GetRequiredService<LoginPage>(), animated: true);
        }
        finally
        {
            _opening = false;
        }
    }

    private async void OnAuthorTapped(object? sender, TappedEventArgs e) =>
        await Launcher.Default.TryOpenAsync(AuthorUrl);
}
