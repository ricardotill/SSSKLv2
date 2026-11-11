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

    /// <summary>Fades the split toggle in or out instead of showing/hiding it, so the order bar never changes height.</summary>
    private async void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PosViewModel.CanSplit))
        {
            return;
        }
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
