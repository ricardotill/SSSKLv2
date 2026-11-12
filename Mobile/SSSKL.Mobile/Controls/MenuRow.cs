using System.Windows.Input;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.Controls.Shapes;
using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Controls;

/// <summary>
/// A tappable row in a grouped list ("Meer", settings, profile): an icon (<see cref="Icons"/>) in a tinted tile, a title with an
/// optional subtitle, an optional detail text or badge, and a chevron. Dims while pressed. Put rows in a
/// <c>Group</c> border; every row but the first draws a divider above itself.
/// </summary>
public sealed class MenuRow : Grid
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(MenuRow), "", propertyChanged: (b, _, n) => ((MenuRow)b)._icon.Icon = (string)n);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(MenuRow), "", propertyChanged: (b, _, _) => ((MenuRow)b).UpdateTexts());

    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(MenuRow), null, propertyChanged: (b, _, _) => ((MenuRow)b).UpdateTexts());

    public static readonly BindableProperty DetailProperty =
        BindableProperty.Create(nameof(Detail), typeof(string), typeof(MenuRow), null, propertyChanged: (b, _, _) => ((MenuRow)b).UpdateTexts());

    public static readonly BindableProperty BadgeProperty =
        BindableProperty.Create(nameof(Badge), typeof(string), typeof(MenuRow), null, propertyChanged: (b, _, _) => ((MenuRow)b).UpdateTexts());

    public static readonly BindableProperty ShowChevronProperty =
        BindableProperty.Create(nameof(ShowChevron), typeof(bool), typeof(MenuRow), true, propertyChanged: (b, _, n) => ((MenuRow)b)._chevron.IsVisible = (bool)n);

    public static readonly BindableProperty IsDestructiveProperty =
        BindableProperty.Create(nameof(IsDestructive), typeof(bool), typeof(MenuRow), false, propertyChanged: (b, _, _) => ((MenuRow)b).UpdateColors());

    public static readonly BindableProperty IsFirstProperty =
        BindableProperty.Create(nameof(IsFirst), typeof(bool), typeof(MenuRow), false, propertyChanged: (b, _, n) => ((MenuRow)b)._divider.IsVisible = !(bool)n);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(MenuRow), null, propertyChanged: (b, _, n) => ((MenuRow)b)._touch.Command = (ICommand?)n);

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(MenuRow), null, propertyChanged: (b, _, n) => ((MenuRow)b)._touch.CommandParameter = n);

    private readonly IconView _icon;
    private readonly Border _iconTile;
    private readonly Label _title;
    private readonly Label _subtitle;
    private readonly Label _detail;
    private readonly Border _badge;
    private readonly Label _badgeText;
    private readonly IconView _chevron;
    private readonly BoxView _divider;
    private readonly TouchBehavior _touch;

    public MenuRow()
    {
        ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto)];
        ColumnSpacing = 12;
        Padding = new Thickness(14, 10);
        MinimumHeightRequest = 52;
        BackgroundColor = Colors.Transparent;

        _icon = new IconView { Size = 18 };
        _iconTile = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 9 },
            WidthRequest = 32,
            HeightRequest = 32,
            VerticalOptions = LayoutOptions.Center,
            Content = _icon
        };

        _title = new Label { FontSize = TypeScale.Callout, LineBreakMode = LineBreakMode.TailTruncation };
        _subtitle = new Label { Style = Resource<Style>("Muted"), LineBreakMode = LineBreakMode.TailTruncation, IsVisible = false };
        var texts = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center, Children = { _title, _subtitle } };

        _detail = new Label { Style = Resource<Style>("Muted"), FontSize = TypeScale.Body, VerticalOptions = LayoutOptions.Center, IsVisible = false };
        _badgeText = new Label { FontAutoScalingEnabled = false, FontSize = TypeScale.Caption, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        _badge = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = Resource<Color>("Danger"),
            Padding = new Thickness(7, 2),
            MinimumWidthRequest = 20,
            HeightRequest = 20,
            VerticalOptions = LayoutOptions.Center,
            Content = _badgeText,
            IsVisible = false
        };
        var trailing = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center, Children = { _detail, _badge } };

        _chevron = new IconView { Icon = Icons.ChevronRight, Size = 16 };
        _chevron.SetAppThemeColor(IconView.ColorProperty, Resource<Color>("MutedLight"), Resource<Color>("MutedDark"));

        // Divider above the row, from the title onwards (like iOS grouped lists).
        _divider = new BoxView { HeightRequest = 1, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(44, -10, -14, 0), InputTransparent = true };
        _divider.SetAppThemeColor(BoxView.ColorProperty, Resource<Color>("BorderLight"), Resource<Color>("BorderDark"));

        this.Add(_iconTile, 0);
        this.Add(texts, 1);
        this.Add(trailing, 2);
        this.Add(_chevron, 3);
        this.Add(_divider, 1);
        Grid.SetColumnSpan((BindableObject)_divider, 3);

        _touch = new TouchBehavior { PressedOpacity = 0.55, DefaultAnimationDuration = 90 };
        Behaviors.Add(_touch);

        UpdateColors();
    }

    public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Subtitle { get => (string?)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string? Detail { get => (string?)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }
    public string? Badge { get => (string?)GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }
    public bool ShowChevron { get => (bool)GetValue(ShowChevronProperty); set => SetValue(ShowChevronProperty, value); }
    public bool IsDestructive { get => (bool)GetValue(IsDestructiveProperty); set => SetValue(IsDestructiveProperty, value); }
    public bool IsFirst { get => (bool)GetValue(IsFirstProperty); set => SetValue(IsFirstProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    private void UpdateTexts()
    {
        _title.Text = Title;
        _subtitle.Text = Subtitle;
        _subtitle.IsVisible = !string.IsNullOrEmpty(Subtitle);
        _detail.Text = Detail;
        _detail.IsVisible = !string.IsNullOrEmpty(Detail);
        _badgeText.Text = Badge;
        _badge.IsVisible = !string.IsNullOrEmpty(Badge);

        // Screen readers: one element with everything the row shows.
        var parts = new[] { Title, Subtitle, Detail, Badge }.Where(p => !string.IsNullOrEmpty(p));
        SemanticProperties.SetDescription(this, string.Join(", ", parts));
    }

    private void UpdateColors()
    {
        if (IsDestructive)
        {
            _title.SetAppThemeColor(Label.TextColorProperty, Resource<Color>("Danger"), Resource<Color>("DangerDark"));
            _icon.SetAppThemeColor(IconView.ColorProperty, Resource<Color>("Danger"), Resource<Color>("DangerDark"));
            _iconTile.SetAppThemeColor(BackgroundColorProperty, Color.FromArgb("#FEF2F2"), Color.FromArgb("#2A1215"));
        }
        else
        {
            _title.SetAppThemeColor(Label.TextColorProperty, Resource<Color>("HeadingLight"), Resource<Color>("HeadingDark"));
            _icon.SetAppThemeColor(IconView.ColorProperty, Resource<Color>("Primary600"), Resource<Color>("Primary400"));
            _iconTile.SetAppThemeColor(BackgroundColorProperty, Resource<Color>("Primary50"), Resource<Color>("Primary950"));
        }
    }

    private static T Resource<T>(string key) => (T)Application.Current!.Resources[key];
}
