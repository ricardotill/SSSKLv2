namespace SSSKL.Mobile.Controls;

/// <summary>
/// Tapping the tab that is already open goes back to that tab's root page (like most iOS and Android apps).
/// Tapping another tab keeps where you were on it. Shell has no hook for this, so the platform shell renderers
/// (<c>Platforms/*/SssklShellRenderer.*.cs</c>) call <see cref="PopToRoot"/> when the open tab is tapped again.
/// </summary>
public static class TabReselect
{
    public static void PopToRoot(ShellSection section)
    {
        if (section.Navigation.NavigationStack.Count <= 1)
        {
            return;
        }
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await section.Navigation.PopToRootAsync();
            }
            catch (Exception ex)
            {
                // A navigation that is already running can refuse the pop; the next tap tries again.
                System.Diagnostics.Debug.WriteLine($"Tab reselect: pop to root failed: {ex}");
            }
        });
    }
}
