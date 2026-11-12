using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Controls;

/// <summary>
/// The empty state of a list: a large muted icon, a title and an optional hint. Every list uses it,
/// so empty pages look the same (put it in <c>CollectionView.EmptyView</c> and bind
/// <see cref="VisualElement.IsVisible"/> to <c>IsEmpty</c>).
/// </summary>
public sealed class EmptyStateView : ContentView
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(EmptyStateView), Icons.List,
            propertyChanged: (b, _, n) => ((EmptyStateView)b)._icon.Icon = (string)n);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(EmptyStateView), string.Empty,
            propertyChanged: (b, _, n) => ((EmptyStateView)b)._title.Text = (string)n);

    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(EmptyStateView), null,
            propertyChanged: (b, _, n) =>
            {
                var view = (EmptyStateView)b;
                view._hint.Text = (string?)n;
                view._hint.IsVisible = !string.IsNullOrEmpty((string?)n);
            });

    private readonly IconView _icon;
    private readonly Label _title;
    private readonly Label _hint;

    public EmptyStateView()
    {
        _icon = new IconView { Icon = Icons.List, Size = 56 };
        _icon.SetAppThemeColor(IconView.ColorProperty, Resource("BorderLight"), Resource("BorderDark"));
        _title = new Label { Style = (Style)Application.Current!.Resources["Title"], HorizontalTextAlignment = TextAlignment.Center };
        _hint = new Label { Style = (Style)Application.Current.Resources["Muted"], HorizontalTextAlignment = TextAlignment.Center, IsVisible = false };

        Content = new VerticalStackLayout
        {
            Padding = new Thickness(32, 64),
            Spacing = 10,
            HorizontalOptions = LayoutOptions.Center,
            MaximumWidthRequest = 420,
            Children = { _icon, _title, _hint }
        };
    }

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
