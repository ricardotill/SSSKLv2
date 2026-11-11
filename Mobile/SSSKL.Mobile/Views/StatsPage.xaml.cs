using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

public partial class StatsPage : ContentPage, INativePage
{
    private readonly StatsViewModel _viewModel;
    private bool _loaded;

    public StatsPage(StatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void Prepare(NativeRoute route) => _viewModel.Initialize(route.UserId);

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
