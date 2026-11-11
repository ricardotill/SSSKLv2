using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

public partial class AchievementsPage : ContentPage, INativePage
{
    private readonly AchievementsViewModel _viewModel;
    private bool _loaded;

    public AchievementsPage(AchievementsViewModel viewModel)
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
