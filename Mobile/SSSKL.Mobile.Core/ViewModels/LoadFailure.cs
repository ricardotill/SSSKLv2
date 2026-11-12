using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// How a page shows a failed load: with nothing on screen yet, an inline error state with a retry button
/// (the page's <c>HasLoadError</c>); with data already shown (a failed refresh), a short toast that keeps the data.
/// No modal alerts for loads.
/// </summary>
public static class LoadFailure
{
    /// <returns>Whether the page should show its inline error state.</returns>
    public static async Task<bool> ReportAsync(IUserDialogs dialogs, IHaptics? haptics, bool hasData)
    {
        haptics?.Error();
        if (hasData)
        {
            await dialogs.ToastAsync(Texts.RefreshFailed);
            return false;
        }
        return true;
    }
}
