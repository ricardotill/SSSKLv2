using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

/// <summary>The "Meer" tab: every SPA feature as a native list; web features open in <see cref="WebPage"/>.</summary>
public partial class MorePage : ContentPage
{
    private readonly MoreViewModel _viewModel;

    public MorePage(MoreViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Saldo and the unread count change elsewhere, so refresh on every visit.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
