using CommunityToolkit.Mvvm.ComponentModel;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Models;

namespace SSSKL.Mobile.Core.Services;

/// <summary>The signed-in user (<c>GET applicationuser/me</c>), shared by POS and Profile. Singleton.</summary>
public sealed partial class CurrentUserService(ISssklApi api) : ObservableObject
{
    [ObservableProperty]
    public partial CurrentUser? User { get; private set; }

    public bool IsGuest => User?.IsInRole(RoleNames.Guest) ?? false;

    public async Task<CurrentUser> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var user = await api.GetMeAsync(cancellationToken);
        User = user;
        return user;
    }

    public void Clear() => User = null;

    partial void OnUserChanged(CurrentUser? value) => OnPropertyChanged(nameof(IsGuest));
}
