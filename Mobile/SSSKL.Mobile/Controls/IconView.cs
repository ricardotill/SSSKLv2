using CommunityToolkit.Maui.Behaviors;

namespace SSSKL.Mobile.Controls;

/// <summary>
/// A Tabler icon (<see cref="Core.Localization.Icons"/>) in a colour, e.g. <c>Color="{AppThemeBinding ...}"</c>.
/// The SVGs are drawn black and tinted at runtime. The tint behavior is set from code, because toolkit
/// behaviors don't inherit bindings. Decorative: hidden from screen readers (the row or button says it).
/// </summary>
public sealed class IconView : Image
{
    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(string), typeof(IconView), null,
            propertyChanged: (b, _, n) => ((IconView)b).Source = n is string name && name.Length > 0 ? ImageSource.FromFile(name) : null);

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(IconView), null,
            propertyChanged: (b, _, n) => ((IconView)b)._tint.TintColor = (Color?)n);

    public static readonly BindableProperty SizeProperty =
        BindableProperty.Create(nameof(Size), typeof(double), typeof(IconView), 18d,
            propertyChanged: (b, _, n) => ((IconView)b).ApplySize((double)n));

    private readonly IconTintColorBehavior _tint = new();

    public IconView()
    {
        Aspect = Aspect.AspectFit;
        VerticalOptions = LayoutOptions.Center;
        HorizontalOptions = LayoutOptions.Center;
        AutomationProperties.SetIsInAccessibleTree(this, false);
        Behaviors.Add(_tint);
        ApplySize(Size);
        // Default: body text colour. A Color set in XAML replaces this.
        this.SetAppThemeColor(ColorProperty,
            (Color)Application.Current!.Resources["TextLight"], (Color)Application.Current.Resources["TextDark"]);
    }

    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public Color? Color { get => (Color?)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
    public double Size { get => (double)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    private void ApplySize(double size)
    {
        WidthRequest = size;
        HeightRequest = size;
    }
}
