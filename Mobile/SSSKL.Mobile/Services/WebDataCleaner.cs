namespace SSSKL.Mobile.Services;

/// <summary>Removes the SPA's localStorage/sessionStorage and cookies from the app's WebViews on sign-out.</summary>
public interface IWebDataCleaner
{
    Task ClearAsync();
}

public sealed partial class WebDataCleaner : IWebDataCleaner
{
    public Task ClearAsync() => MainThread.InvokeOnMainThreadAsync(ClearPlatformAsync);

    private partial Task ClearPlatformAsync();
}
