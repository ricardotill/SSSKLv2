using Microsoft.Maui.Controls.Shapes;
using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Controls;

/// <summary>
/// An achievement tier as a small gradient pill, in the SPA's tier colours (shared/components/tier-badge):
/// bronze, silver, gold and platinum. Text colours keep WCAG AA contrast on their gradient.
/// </summary>
public sealed class TierBadge : Border
{
    public static readonly BindableProperty TierProperty =
        BindableProperty.Create(nameof(Tier), typeof(string), typeof(TierBadge), "",
            propertyChanged: (b, _, n) => ((TierBadge)b).Apply((string)n));

    private readonly Label _label;

    public TierBadge()
    {
        StrokeThickness = 0;
        StrokeShape = new RoundRectangle { CornerRadius = 10 };
        Padding = new Thickness(9, 3);
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        _label = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold, CharacterSpacing = 0.5 };
        Content = _label;
        Apply("");
    }

    public string Tier { get => (string)GetValue(TierProperty); set => SetValue(TierProperty, value); }

    private void Apply(string tier)
    {
        var (text, from, to, textColor) = tier switch
        {
            "Bronze" => (Texts.TierBronze, "#B45309", "#78350F", Colors.White),
            "Silver" => (Texts.TierSilver, "#E2E8F0", "#94A3B8", Color.FromArgb("#0F172A")),
            "Gold" => (Texts.TierGold, "#FDE047", "#F59E0B", Color.FromArgb("#422006")),
            "Platinum" => (Texts.TierPlatinum, "#A21CAF", "#4338CA", Colors.White),
            _ => ("", "#00000000", "#00000000", Colors.Transparent)
        };
        IsVisible = text.Length > 0;
        _label.Text = text.ToUpperInvariant();
        _label.TextColor = textColor;
        Background = new LinearGradientBrush(
            [new GradientStop(Color.FromArgb(from), 0), new GradientStop(Color.FromArgb(to), 1)],
            new Point(0, 0), new Point(1, 1));
        SemanticProperties.SetDescription(this, text);
    }
}
