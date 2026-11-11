using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersViewModel _viewModel;
    private bool _loaded;

    public OrdersPage(OrdersViewModel viewModel)
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
