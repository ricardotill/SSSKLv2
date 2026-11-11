using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Services;

public sealed class UserDialogs : IUserDialogs
{
    public Task ToastAsync(string message) =>
        MainThread.InvokeOnMainThreadAsync(() => Toast.Make(message, ToastDuration.Short).Show());

    public Task AlertAsync(string title, string message) =>
        MainThread.InvokeOnMainThreadAsync(() => CurrentPage?.DisplayAlertAsync(title, message, Texts.Ok) ?? Task.CompletedTask);

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        MainThread.InvokeOnMainThreadAsync(() => CurrentPage?.DisplayAlertAsync(title, message, accept, cancel) ?? Task.FromResult(false));

    private static Page? CurrentPage
    {
        get
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            return page?.Navigation.ModalStack.LastOrDefault() ?? page;
        }
    }
}
