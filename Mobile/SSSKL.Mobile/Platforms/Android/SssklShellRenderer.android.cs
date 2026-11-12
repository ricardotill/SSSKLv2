using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using SSSKL.Mobile.Controls;

namespace SSSKL.Mobile;

/// <summary>Shell renderer whose bottom tabs go back to the tab's root page when the open tab is tapped again.</summary>
public sealed class SssklShellRenderer : ShellRenderer
{
    protected override IShellItemRenderer CreateShellItemRenderer(ShellItem shellItem) => new SssklShellItemRenderer(this);

    private sealed class SssklShellItemRenderer(IShellContext context) : ShellItemRenderer(context)
    {
        // MAUI calls this (an empty hook by default) when the checked bottom-navigation item is tapped.
        protected override void OnTabReselected(ShellSection shellSection) => TabReselect.PopToRoot(shellSection);
    }
}
