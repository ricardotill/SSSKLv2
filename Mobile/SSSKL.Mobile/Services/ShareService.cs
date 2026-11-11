using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Services;

public sealed class ShareService : IShareService
{
    public Task ShareAsync(string title, string text, Uri url) =>
        MainThread.InvokeOnMainThreadAsync(() => Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = title,
            Text = text,
            Uri = url.ToString()
        }));
}

public sealed class ExternalLauncher : IExternalLauncher
{
    public Task OpenAsync(Uri url) => MainThread.InvokeOnMainThreadAsync(() => Launcher.Default.TryOpenAsync(url));
}
