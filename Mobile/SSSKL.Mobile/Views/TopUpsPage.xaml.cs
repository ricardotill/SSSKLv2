using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

public partial class TopUpsPage : ContentPage
{
    private readonly TopUpsViewModel _viewModel;
    private bool _loaded;

    public TopUpsPage(TopUpsViewModel viewModel)
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
