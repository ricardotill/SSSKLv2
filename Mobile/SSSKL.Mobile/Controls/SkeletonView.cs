namespace SSSKL.Mobile.Controls;

public enum SkeletonKind
{
    /// <summary>Card rows with an avatar and two lines (lists).</summary>
    Rows,
    /// <summary>Two columns of tiles, then rows (Kassa).</summary>
    Tiles,
    /// <summary>A large avatar, a name and stat cards (profile, statistics).</summary>
    Profile
}

/// <summary>
/// Grey placeholders in the shape of the page, shown while it loads for the first time (bind
/// <see cref="VisualElement.IsVisible"/> to the view model's <c>ShowSkeleton</c>). They pulse gently while
/// visible. Covers the page with the page colour, so an empty view behind it doesn't flash. Hidden from
/// screen readers, which hear "Laden" instead.
/// </summary>
public sealed class SkeletonView : ContentView
{
    public static readonly BindableProperty KindProperty =
        BindableProperty.Create(nameof(Kind), typeof(SkeletonKind), typeof(SkeletonView), SkeletonKind.Rows,
            propertyChanged: (b, _, _) => ((SkeletonView)b).Build());

    private const string PulseAnimation = "SkeletonPulse";

    public SkeletonView()
    {
        this.SetAppThemeColor(BackgroundColorProperty, Resource("PageLight"), Resource("PageDark"));
        SemanticProperties.SetDescription(this, "Laden");
        Build();
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IsVisible))
            {
                UpdatePulse();
            }
        };
        Loaded += (_, _) => UpdatePulse();
        Unloaded += (_, _) => this.AbortAnimation(PulseAnimation);
    }

    public SkeletonKind Kind { get => (SkeletonKind)GetValue(KindProperty); set => SetValue(KindProperty, value); }

    private void Build()
    {
        var stack = new VerticalStackLayout { Padding = new Thickness(16, 8), Spacing = 8 };
        switch (Kind)
        {
            case SkeletonKind.Tiles:
                stack.Add(Bar(110, 18, new Thickness(0, 8, 0, 4)));
                var tiles = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)], ColumnSpacing = 8, RowSpacing = 8 };
                for (var i = 0; i < 6; i++)
                {
                    tiles.Add(Card(56), i % 2, i / 2);
                }
                stack.Add(tiles);
                stack.Add(Bar(90, 18, new Thickness(0, 12, 0, 4)));
                for (var i = 0; i < 3; i++)
                {
                    stack.Add(RowCard());
                }
                break;
            case SkeletonKind.Profile:
                stack.Add(Circle(112, LayoutOptions.Center));
                stack.Add(Bar(160, 22, new Thickness(0, 8, 0, 0), LayoutOptions.Center));
                stack.Add(Bar(100, 14, default, LayoutOptions.Center));
                var stats = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 8, Margin = new Thickness(0, 12, 0, 0) };
                for (var i = 0; i < 3; i++)
                {
                    stats.Add(Card(64), i);
                }
                stack.Add(stats);
                stack.Add(Card(96));
                break;
            default:
                for (var i = 0; i < 8; i++)
                {
                    stack.Add(RowCard());
                }
                break;
        }
        Content = stack;
    }

    private static Border RowCard()
    {
        var lines = new VerticalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center, Children = { Bar(140, 14), Bar(90, 11) } };
        var grid = new Grid { ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)], ColumnSpacing = 12 };
        grid.Add(Circle(40), 0);
        grid.Add(lines, 1);
        grid.Add(Bar(48, 14), 2);
        var card = Card(64);
        card.Content = grid;
        card.Padding = new Thickness(14, 12);
        return card;
    }

    private static Border Card(double height)
    {
        var card = new Border
        {
            HeightRequest = height,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }
        };
        card.SetAppThemeColor(BackgroundColorProperty, Resource("SurfaceLight"), Resource("SurfaceDark"));
        card.SetAppThemeColor(Border.StrokeProperty, Resource("BorderLight"), Resource("BorderDark"));
        return card;
    }

    private static Border Bar(double width, double height, Thickness margin = default, LayoutOptions? horizontal = null) =>
        Block(width, height, height / 2, margin, horizontal ?? LayoutOptions.Start);

    private static Border Circle(double size, LayoutOptions? horizontal = null) =>
        Block(size, size, size / 2, default, horizontal ?? LayoutOptions.Start);

    private static Border Block(double width, double height, double radius, Thickness margin, LayoutOptions horizontal)
    {
        var block = new Border
        {
            WidthRequest = width,
            HeightRequest = height,
            Margin = margin,
            HorizontalOptions = horizontal,
            VerticalOptions = LayoutOptions.Center,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = radius }
        };
        block.SetAppThemeColor(BackgroundColorProperty, Resource("SunkenLight"), Resource("SunkenDark"));
        return block;
    }

    private void UpdatePulse()
    {
        this.AbortAnimation(PulseAnimation);
        if (!IsVisible || Content is null)
        {
            if (Content is not null)
            {
                Content.Opacity = 1;
            }
            return;
        }
        var content = Content;
        new Animation
        {
            { 0, 0.5, new Animation(v => content.Opacity = v, 1, 0.55, Easing.SinInOut) },
            { 0.5, 1, new Animation(v => content.Opacity = v, 0.55, 1, Easing.SinInOut) }
        }.Commit(this, PulseAnimation, length: 1400, repeat: () => IsVisible);
    }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
