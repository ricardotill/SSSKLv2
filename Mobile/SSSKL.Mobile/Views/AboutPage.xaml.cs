using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Views;

public partial class AboutPage : ContentPage
{
    private static readonly Uri AuthorUrl = new("https://ricardotill.nl");
    private static readonly Uri GitHubUrl = new("https://github.com/ricardotill/SSSKLv2");

    public AboutPage()
    {
        InitializeComponent();
        VersionLabel.Text = $"v{AppInfo.Current.VersionString}";
        GitHubButton.Command = new Command(async () => await Launcher.Default.TryOpenAsync(GitHubUrl));
    }

    private async void OnAuthorTapped(object? sender, TappedEventArgs e) => await Launcher.Default.TryOpenAsync(AuthorUrl);

    private async void OnMailTapped(object? sender, TappedEventArgs e) =>
        await Launcher.Default.TryOpenAsync(new Uri($"mailto:{Texts.ContactEmail}"));
}
