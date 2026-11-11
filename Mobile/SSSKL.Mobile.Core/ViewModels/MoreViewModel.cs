using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Navigation;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>The "Meer" tab: who is signed in, and every feature without a tab of its own.</summary>
public sealed partial class MoreViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    ILogger<MoreViewModel> logger) : ObservableObject
{
    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");

    // Last known values, so the cached menu matches the previous visit until the refresh is done.
    private bool _canAccessQuotes = true;

    public ObservableCollection<MenuSection> Sections { get; } = [];

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    public partial string DisplayName { get; set; } = "";

    [ObservableProperty]
    public partial string Initials { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPicture))]
    public partial string? PictureUrl { get; set; }

    [ObservableProperty]
    public partial string Saldo { get; set; } = "";

    [ObservableProperty]
    public partial bool IsSaldoNegative { get; set; }

    public bool HasPicture => PictureUrl is not null;

    /// <summary>
    /// Shows the cached user (and menu) at once, then refreshes it and the quotes access.
    /// Failures are logged and leave what's shown: the menu works without them.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            if (currentUser.User is { } cached)
            {
                Apply(cached);
            }

            var userTask = currentUser.RefreshAsync();
            var quotesTask = TryAsync(() => api.CanAccessQuotesAsync(), fallback: true, "Probing quotes access failed");
            CurrentUser user;
            try
            {
                user = await userTask;
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
            {
                logger.LogWarning(ex, "Loading the current user failed");
                if (currentUser.User is null)
                {
                    await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
                    return;
                }
                user = currentUser.User;
            }
            _canAccessQuotes = await quotesTask;
            Apply(user);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task OpenAsync(MenuEntry entry) => navigator.OpenAsync(entry.Path);

    [RelayCommand]
    private Task OpenProfileAsync() => navigator.OpenProfileAsync();

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (await dialogs.ConfirmAsync(Texts.Logout, Texts.ConfirmLogout, Texts.Logout, Texts.Cancel))
        {
            await navigator.SignOutAsync();
        }
    }

    private void Apply(CurrentUser user)
    {
        DisplayName = user.DisplayName;
        Initials = UserItemViewModel.InitialsOf(user.DisplayName);
        PictureUrl = endpoints.ResolveApiUrl(user.ProfilePictureUrl);
        Saldo = user.Saldo.ToString("C", Dutch);
        IsSaldoNegative = user.Saldo < 0;

        var sections = MoreMenu.Build(user, _canAccessQuotes);
        // Only replace the list when it changed, so the page doesn't flicker on every visit.
        if (!Sections.SequenceEqual(sections, MenuSectionComparer.Instance))
        {
            Sections.Clear();
            foreach (var section in sections)
            {
                Sections.Add(section);
            }
        }
    }

    private async Task<T> TryAsync<T>(Func<Task<T>> call, T fallback, string message)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "{Message}", message);
            return fallback;
        }
    }

    private sealed class MenuSectionComparer : IEqualityComparer<MenuSection>
    {
        public static readonly MenuSectionComparer Instance = new();

        public bool Equals(MenuSection? x, MenuSection? y) =>
            x is not null && y is not null && x.Title == y.Title && x.Entries.SequenceEqual(y.Entries);

        public int GetHashCode(MenuSection obj) => obj.Title.GetHashCode(StringComparison.Ordinal);
    }
}
