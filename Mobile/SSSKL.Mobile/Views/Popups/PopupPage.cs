using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Application = Microsoft.Maui.Controls.Application;
using Microsoft.Maui.Controls.Shapes;
using SSSKL.Mobile.Core.Localization;

namespace SSSKL.Mobile.Views.Popups;

/// <summary>A dimmed, transparent modal page with a centered card; <see cref="Closed"/> completes when dismissed.</summary>
public abstract class PopupPage : ContentPage
{
    private readonly TaskCompletionSource _closed = new();
    private readonly Border _card;

    protected PopupPage()
    {
        BackgroundColor = Color.FromRgba(0, 0, 0, 0.55);
        Shell.SetPresentationMode(this, PresentationMode.ModalNotAnimated);
        On<iOS>().SetModalPresentationStyle(UIModalPresentationStyle.OverFullScreen);

        _card = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(20),
            Margin = new Thickness(24),
            MaximumWidthRequest = 480,
            VerticalOptions = LayoutOptions.Center,
            Opacity = 0,
            Scale = 0.9
        };
        _card.SetAppThemeColor(BackgroundColorProperty, (Color)Application.Current!.Resources["SurfaceLight"], (Color)Application.Current.Resources["SurfaceDark"]);
        Content = _card;
    }

    public Task Closed => _closed.Task;

    protected void SetCardContent(View content) => _card.Content = content;

    protected Button CreateCloseButton(string? text = null)
    {
        var button = new Button { Text = text ?? Texts.Close, Margin = new Thickness(0, 16, 0, 0) };
        button.Clicked += async (_, _) => await CloseAsync();
        return button;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.WhenAll(_card.FadeToAsync(1, 180), _card.ScaleToAsync(1, 220, Easing.CubicOut));
    }

    protected override bool OnBackButtonPressed()
    {
        _ = CloseAsync();
        return true;
    }

    public async Task CloseAsync()
    {
        if (_closed.Task.IsCompleted)
        {
            return;
        }
        if (Navigation.ModalStack.Contains(this))
        {
            await Navigation.PopModalAsync(animated: false);
        }
        _closed.TrySetResult();
    }
}
