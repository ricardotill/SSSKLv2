using Microsoft.Maui.Controls.Shapes;

namespace SSSKL.Mobile.Controls;

/// <summary>A round avatar: the member's picture, or their initials on a tinted circle.</summary>
public sealed class AvatarView : Grid
{
    public static readonly BindableProperty InitialsProperty =
        BindableProperty.Create(nameof(Initials), typeof(string), typeof(AvatarView), "",
            propertyChanged: (b, _, n) => ((AvatarView)b)._initials.Text = (string)n);

    public static readonly BindableProperty PictureUrlProperty =
        BindableProperty.Create(nameof(PictureUrl), typeof(string), typeof(AvatarView), null,
            propertyChanged: (b, _, n) => ((AvatarView)b).UpdatePicture((string?)n));

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(AvatarView), 40d,
            propertyChanged: (b, _, n) => ((AvatarView)b).ApplySize((double)n));

    private readonly Label _initials;
    private readonly FadeImage _picture;
    private readonly Border _circle;
    private readonly Border _pictureCircle;

    public AvatarView()
    {
        _initials = new Label { FontAutoScalingEnabled = false, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        _initials.SetAppThemeColor(Label.TextColorProperty, Resource("Primary700"), Resource("Primary300"));
        _circle = new Border { StrokeThickness = 0, Content = _initials };
        _circle.SetAppThemeColor(BackgroundColorProperty, Resource("Primary100"), Resource("Primary900"));

        _picture = new FadeImage { Aspect = Aspect.AspectFill };
        _pictureCircle = new Border { StrokeThickness = 0, Content = _picture, IsVisible = false };

        Children.Add(_circle);
        Children.Add(_pictureCircle);
        // Decorative: the row around it names the member.
        AutomationProperties.SetIsInAccessibleTree(this, false);
        ApplySize(Size);
    }

    public string Initials { get => (string)GetValue(InitialsProperty); set => SetValue(InitialsProperty, value); }
    public string? PictureUrl { get => (string?)GetValue(PictureUrlProperty); set => SetValue(PictureUrlProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private void UpdatePicture(string? url)
    {
        _picture.Source = url is null ? null : ImageSource.FromUri(new Uri(url));
        _pictureCircle.IsVisible = url is not null;
    }

    private void ApplySize(double size)
    {
        WidthRequest = HeightRequest = size;
        var shape = new RoundRectangle { CornerRadius = size / 2 };
        _circle.StrokeShape = shape;
        _pictureCircle.StrokeShape = new RoundRectangle { CornerRadius = size / 2 };
        _initials.FontSize = Math.Max(12, size * 0.38);
    }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
