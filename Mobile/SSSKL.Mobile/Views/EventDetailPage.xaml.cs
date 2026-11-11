using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

public partial class EventDetailPage : ContentPage, INativePage
{
    private readonly EventDetailViewModel _viewModel;

    public EventDetailPage(EventDetailViewModel viewModel)
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

    /// <summary>
    /// Loads on the first visit, and again on every return: the SPA's form in the WebView (new, edit) may
    /// have changed what this page shows.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_viewModel.IsLoading)
        {
            await _viewModel.LoadAsync();
        }
    }
}
