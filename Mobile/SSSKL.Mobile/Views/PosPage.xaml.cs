using System.ComponentModel;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

public partial class PosPage : ContentPage
{
    private readonly PosViewModel _viewModel;
    private bool _loaded;

    public PosPage(PosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelChanged;
    }

    private async void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PosViewModel.CanSplit):
                await FadeSplitAsync();
                break;
            case nameof(PosViewModel.ShowOrderPlaced):
                await FadeOrderPlacedAsync();
                break;
        }
    }

    /// <summary>The order confirmation fades in over the pay panel, its check pops; it fades out again.</summary>
    private async Task FadeOrderPlacedAsync()
    {
        var show = _viewModel.ShowOrderPlaced;
        OrderPlacedPanel.InputTransparent = !show;
        AutomationProperties.SetIsInAccessibleTree(OrderPlacedPanel, show);
        OrderPlacedPanel.CancelAnimations();
        OrderPlacedBadge.CancelAnimations();
        if (!show)
        {
            await OrderPlacedPanel.FadeToAsync(0, 200, Easing.CubicIn);
            return;
        }
        OrderPlacedBadge.Scale = 0.6;
        SemanticScreenReader.Announce($"{Core.Localization.Texts.OrderPlaced} {_viewModel.OrderPlacedDetail}");
        await Task.WhenAll(
            OrderPlacedPanel.FadeToAsync(1, 160, Easing.CubicOut),
            OrderPlacedBadge.ScaleToAsync(1, 320, Easing.SpringOut));
    }

    /// <summary>Fades the split toggle in or out instead of showing/hiding it, so the order bar never changes height.</summary>
    private async Task FadeSplitAsync()
    {
        var canSplit = _viewModel.CanSplit;
        SplitPanel.InputTransparent = !canSplit;
        AutomationProperties.SetIsInAccessibleTree(SplitPanel, canSplit);
        SplitPanel.CancelAnimations();
        await SplitPanel.FadeToAsync(canSplit ? 1 : 0, 150, Easing.CubicOut);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await _viewModel.LoadAsync();
        }
    }
}
