using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

public partial class UserProfilePage : ContentPage, INativePage
{
    private readonly UserProfileViewModel _viewModel;
    private bool _loaded;

    public UserProfilePage(UserProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void Prepare(NativeRoute route) => _viewModel.Initialize(route.Id ?? "");

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
