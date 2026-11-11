using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

public partial class AchievementDetailPage : ContentPage, INativePage
{
    private readonly AchievementDetailViewModel _viewModel;
    private bool _loaded;

    public AchievementDetailPage(AchievementDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public NativeRoute? Route { get; private set; }

    public void Prepare(NativeRoute route)
    {
        Route = route;
        _viewModel.Initialize(Guid.TryParse(route.Id, out var id) ? id : Guid.Empty);
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
