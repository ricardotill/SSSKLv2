using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Views;

/// <summary>Native settings, opened from "Meer" and from the profile. A new page per visit.</summary>
public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        viewModel.AppVersion = AppInfo.Current.VersionString;
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Reload on every visit: the e-mail or 2FA status may have changed in the web settings.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
