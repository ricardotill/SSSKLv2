using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>
/// "Saldolijst" (SPA users): every member with their saldo, lowest first like the SPA, with a search box.
/// Tapping a member opens their profile.
/// </summary>
public sealed partial class SaldoListViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    ILogger<SaldoListViewModel> logger) : ObservableObject
{
    /// <summary>The SPA loads everyone at once too.</summary>
    public const int MaxUsers = 1000;

    private List<MemberRow> _all = [];

    public ObservableCollection<MemberRow> Members { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(ShowSkeleton))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(ShowSkeleton))]
    public partial bool HasLoaded { get; set; }

    /// <summary>The first load failed: the page shows its error state with a retry button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSkeleton))]
    public partial bool HasLoadError { get; set; }

    /// <summary>Placeholders while the first load runs (not while refreshing loaded data).</summary>
    public bool ShowSkeleton => IsLoading && !HasLoaded && !HasLoadError;

    [ObservableProperty]
    public partial string Query { get; set; } = "";

    public bool IsEmpty => HasLoaded && !IsLoading && Members.Count == 0;

    [RelayCommand]
    public async Task LoadAsync()
    {
        HasLoadError = false;
        IsLoading = true;
        try
        {
            var me = currentUser.User?.Id;
            var page = await api.GetUsersAsync(0, MaxUsers);
            _all = page.Items
                .OrderBy(u => u.Saldo)
                .ThenBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .Select(u => new MemberRow(u.Id, u.DisplayName, endpoints.ResolveApiUrl(u.ProfilePictureUrl),
                    Display.Money(u.Saldo), IsSelf: u.Id == me, IsDetailNegative: u.Saldo < 0))
                .ToList();
            HasLoaded = true;
            ApplyFilter();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Loading the saldo list failed");
            HasLoadError = await LoadFailure.ReportAsync(dialogs, null, HasLoaded);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    [RelayCommand]
    private Task OpenAsync(MemberRow member) =>
        member.UserId is null ? Task.CompletedTask : navigator.OpenAsync(NativeRoutes.UserProfilePath(member.UserId));

    partial void OnQueryChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = Query.Trim();
        Members.Clear();
        foreach (var member in _all.Where(m => query.Length == 0 || m.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)))
        {
            Members.Add(member);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }
}
