using Microsoft.Maui.Controls.Shapes;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views.Popups;

/// <summary>New achievements, swipeable when there are several.</summary>
public sealed class AchievementPopupPage : PopupPage
{
    public AchievementPopupPage(IReadOnlyList<AchievementItem> items)
    {
        var carousel = new CarouselView
        {
            ItemsSource = items,
            HeightRequest = 280,
            Loop = false,
            ItemTemplate = new DataTemplate(CreateItemView)
        };
        var indicator = new IndicatorView
        {
            HorizontalOptions = LayoutOptions.Center,
            IndicatorColor = Colors.Gray,
            SelectedIndicatorColor = (Color)Application.Current!.Resources["Primary500"],
            IsVisible = items.Count > 1
        };
        carousel.IndicatorView = indicator;

        SetCardContent(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = items.Count > 1 ? Texts.NewAchievementsTitle : Texts.NewAchievementTitle,
                    Style = (Style)Application.Current.Resources["Headline"],
                    HorizontalTextAlignment = TextAlignment.Center
                },
                new Label { Text = Texts.NewAchievementDescription, HorizontalTextAlignment = TextAlignment.Center, Style = (Style)Application.Current.Resources["Muted"] },
                carousel,
                indicator,
                CreateCloseButton()
            }
        });
        SemanticProperties.SetDescription(this, Texts.NewAchievementTitle);
    }

    private static View CreateItemView()
    {
        var image = new Image { HeightRequest = 140, WidthRequest = 140, Aspect = Aspect.AspectFit };
        image.SetBinding(Image.SourceProperty, static (AchievementItem a) => a.ImageUrl);
        var imageFrame = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 70 },
            HorizontalOptions = LayoutOptions.Center,
            Content = image
        };

        var name = new Label { Style = (Style)Application.Current!.Resources["Title"], HorizontalTextAlignment = TextAlignment.Center };
        name.SetBinding(Label.TextProperty, static (AchievementItem a) => a.Name);
        var tier = new Label { Style = (Style)Application.Current.Resources["Muted"], HorizontalTextAlignment = TextAlignment.Center };
        tier.SetBinding(Label.TextProperty, static (AchievementItem a) => a.Tier);
        var description = new Label { HorizontalTextAlignment = TextAlignment.Center };
        description.SetBinding(Label.TextProperty, static (AchievementItem a) => a.Description);

        return new VerticalStackLayout { Spacing = 6, Padding = new Thickness(4), Children = { imageFrame, name, tier, description } };
    }
}
