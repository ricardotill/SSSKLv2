using System.Windows.Input;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Controls;

/// <summary>
/// A <see cref="MemberRow"/> as a tappable card: optional rank (gold, silver, bronze for the top three),
/// avatar, name, optional subtitle and a value on the right. Your own row is tinted. Its BindingContext is
/// the row; <see cref="Command"/> gets the row as parameter.
/// </summary>
public sealed class MemberRowView : ContentView
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(MemberRowView), null,
            propertyChanged: (b, _, n) => ((MemberRowView)b)._pressable.Command = (ICommand?)n);

    private readonly Pressable _pressable;
    private readonly Border _card;
    private readonly Border _rank;
    private readonly Label _rankText;
    private readonly AvatarView _avatar;
    private readonly Label _name;
    private readonly Label _subtitle;
    private readonly Label _detail;

    public MemberRowView()
    {
        _rankText = new Label { FontAttributes = FontAttributes.Bold, FontSize = 14, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        _rank = new Border
        {
            WidthRequest = 32, HeightRequest = 32, StrokeThickness = 0, VerticalOptions = LayoutOptions.Center,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 }, Content = _rankText
        };
        _avatar = new AvatarView { Size = 40, VerticalOptions = LayoutOptions.Center };
        _name = new Label { FontSize = 16, LineBreakMode = LineBreakMode.TailTruncation };
        _name.SetAppThemeColor(Label.TextColorProperty, Resource("HeadingLight"), Resource("HeadingDark"));
        _subtitle = new Label { Style = (Style)Application.Current!.Resources["Muted"], FontSize = 13, LineBreakMode = LineBreakMode.TailTruncation };
        _detail = new Label { FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.End };

        var texts = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center, Children = { _name, _subtitle } };
        var grid = new Grid
        {
            ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 12
        };
        grid.Add(_rank, 0);
        grid.Add(_avatar, 1);
        grid.Add(texts, 2);
        grid.Add(_detail, 3);

        _card = new Border { Style = (Style)Application.Current.Resources["Card"], Padding = new Thickness(12, 10), Content = grid };
        _pressable = new Pressable { Padding = new Thickness(16, 0), Content = _card };
        Content = _pressable;
    }

    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is not MemberRow row)
        {
            return;
        }
        _pressable.CommandParameter = row;
        _pressable.IsEnabled = row.CanOpen;
        SemanticProperties.SetDescription(_pressable, row.Description);

        _rank.IsVisible = row.Rank is not null;
        _rankText.Text = row.RankText;
        var (rankBackground, rankText) = row switch
        {
            { IsGold: true } => (Color.FromArgb("#F59E0B"), Color.FromArgb("#422006")),
            { IsSilver: true } => (Color.FromArgb("#CBD5E1"), Color.FromArgb("#0F172A")),
            { IsBronze: true } => (Color.FromArgb("#B45309"), Colors.White),
            _ => (Colors.Transparent, (Color?)null)
        };
        _rank.BackgroundColor = rankBackground;
        if (rankText is null)
        {
            _rankText.SetAppThemeColor(Label.TextColorProperty, Resource("MutedLight"), Resource("MutedDark"));
        }
        else
        {
            _rankText.TextColor = rankText;
        }

        _avatar.Initials = row.Initials;
        _avatar.PictureUrl = row.PictureUrl;
        _name.Text = row.Name;
        _name.FontAttributes = row.IsSelf ? FontAttributes.Bold : FontAttributes.None;
        _subtitle.Text = row.Subtitle;
        _subtitle.IsVisible = row.HasSubtitle;
        _detail.Text = row.Detail;
        if (row.IsDetailNegative)
        {
            _detail.SetAppThemeColor(Label.TextColorProperty, Resource("Danger"), Resource("DangerDark"));
        }
        else
        {
            _detail.SetAppThemeColor(Label.TextColorProperty, Resource("TextLight"), Resource("TextDark"));
        }

        // Your own row stands out, like the selected tiles in Kassa.
        if (row.IsSelf)
        {
            _card.SetAppThemeColor(BackgroundColorProperty, Resource("Primary50"), Resource("Primary950"));
            _card.Stroke = new SolidColorBrush(Resource("Primary500"));
        }
        else
        {
            _card.SetAppThemeColor(BackgroundColorProperty, Resource("SurfaceLight"), Resource("SurfaceDark"));
            _card.SetAppTheme<Brush>(Border.StrokeProperty, new SolidColorBrush(Resource("BorderLight")), new SolidColorBrush(Resource("BorderDark")));
        }
    }

    private static Color Resource(string key) => (Color)Application.Current!.Resources[key];
}
