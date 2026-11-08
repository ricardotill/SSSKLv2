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
