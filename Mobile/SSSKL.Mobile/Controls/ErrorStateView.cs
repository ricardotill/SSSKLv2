using System.Windows.Input;
using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Controls;

/// <summary>
/// A page's load error: icon, "Laden van gegevens mislukt", a hint and "Opnieuw proberen" (bind
/// <see cref="RetryCommand"/> to the page's load command and <see cref="VisualElement.IsVisible"/> to
/// <c>HasLoadError</c>). Covers the page with the page colour.
/// </summary>
public sealed class ErrorStateView : ContentView
{
    public static readonly BindableProperty RetryCommandProperty =
        BindableProperty.Create(nameof(RetryCommand), typeof(ICommand), typeof(ErrorStateView), null,
            propertyChanged: (b, _, n) => ((ErrorStateView)b)._retry.Command = (ICommand?)n);

    private readonly Button _retry;

    public ErrorStateView()
    {
        this.SetAppThemeColor(BackgroundColorProperty, Resource("PageLight"), Resource("PageDark"));

        var icon = new IconView { Icon = Icons.CloudOff, Size = 56 };
        icon.SetAppThemeColor(IconView.ColorProperty, Resource("MutedLight"), Resource("MutedDark"));
        var title = new Label { Text = Texts.LoadFailed, Style = (Style)Application.Current!.Resources["Title"], HorizontalTextAlignment = TextAlignment.Center };
        SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level2);
        var hint = new Label { Text = Texts.LoadFailedHint, Style = (Style)Application.Current.Resources["Muted"], HorizontalTextAlignment = TextAlignment.Center };
        _retry = new Button { Text = Texts.Retry, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 8, 0, 0) };
        _retry.Behaviors.Add(new PressScaleBehavior());

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(32),
            Spacing = 10,
            VerticalOptions = LayoutOptions.Center,
            MaximumWidthRequest = 420,
            Children = { icon, title, hint, _retry }
        };
    }

    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
