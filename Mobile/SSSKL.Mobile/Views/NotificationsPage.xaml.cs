using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

public partial class NotificationsPage : ContentPage
{
    private readonly NotificationsViewModel _viewModel;

    public NotificationsPage(NotificationsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Reloads the first page on every visit: new notifications are at the top.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_viewModel.IsLoading)
        {
            await _viewModel.LoadAsync();
        }
    }
}
