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
/// One achievement (SPA achievements/{id}): image, tier, description, whether you earned it, the parent
/// achievement, and everyone who earned it (first earner first).
/// </summary>
public sealed partial class AchievementDetailViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    ILogger<AchievementDetailViewModel> logger) : ObservableObject
{
    private Guid _id;
    private Guid? _parentId;

    public ObservableCollection<MemberRow> Earners { get; } = [];

    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Description { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage), nameof(HasNoImage))]
    public partial string? ImageUrl { get; set; }

    [ObservableProperty]
    public partial string Tier { get; set; } = "";

    [ObservableProperty]
    public partial string StatusText { get; set; } = "";

    [ObservableProperty]
    public partial bool Completed { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasParent))]
    public partial string? ParentText { get; set; }

    [ObservableProperty]
    public partial string EarnedByText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFound))]
    public partial bool IsNotFound { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFound))]
    public partial bool HasLoaded { get; set; }

    [ObservableProperty]
    public partial bool HasNoEarners { get; set; }

    public bool HasImage => ImageUrl is not null;
    public bool HasNoImage => ImageUrl is null;
    public bool HasParent => ParentText is not null;
    public bool IsFound => HasLoaded && !IsNotFound;

    public void Initialize(Guid id) => _id = id;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var me = currentUser.User ?? await currentUser.RefreshAsync();
            var earnersTask = api.GetAchievementEarnersAsync(_id);
            var listingTask = api.GetAchievementListingAsync(me.Id);
            var earners = await earnersTask;
            var listing = await listingTask;

            // The listing has the achievement even without earners; the earners are the fallback.
            if (listing.FirstOrDefault(a => a.Id == _id) is { } achievement)
            {
                Name = achievement.Name;
                Description = achievement.Description;
                ImageUrl = endpoints.ResolveApiUrl(achievement.ImageUrl);
                Tier = achievement.Tier;
                Completed = achievement.Completed;
                StatusText = achievement.Completed && achievement.DateAdded is { } date ? Texts.AchievedOn(Display.Date(date)) : Texts.NotAchievedYet;
                _parentId = achievement.ParentAchievementId;
                ParentText = achievement.ParentAchievementName is { Length: > 0 } parent ? Texts.PartOf(parent) : null;
            }
            else if (earners.Count > 0)
            {
                var first = earners[0];
                Name = first.AchievementName;
                Description = first.AchievementDescription;
                ImageUrl = endpoints.ResolveApiUrl(first.ImageUrl);
                Tier = first.Tier;
                Completed = earners.Any(e => e.UserId == me.Id);
                StatusText = Completed ? Texts.FilterEarned : Texts.NotAchievedYet;
                _parentId = first.ParentAchievementId;
                ParentText = first.ParentAchievementName is { Length: > 0 } parent ? Texts.PartOf(parent) : null;
            }
            else
            {
                IsNotFound = true;
            }

            Earners.Clear();
            foreach (var earner in earners.OrderBy(e => e.DateAdded))
            {
                var name = earner.UserFullName is { Length: > 0 } full ? full : earner.UserName ?? "?";
                Earners.Add(new MemberRow(earner.UserId, name, endpoints.ResolveApiUrl(earner.UserProfilePictureUrl),
                    Display.Date(earner.DateAdded), IsSelf: earner.UserId == me.Id,
                    Subtitle: earner.UserName is { Length: > 0 } userName && earner.UserFullName is { Length: > 0 } ? "@" + userName : null));
            }
            EarnedByText = Texts.EarnedByCount(Earners.Count);
            HasNoEarners = Earners.Count == 0 && !IsNotFound;
            HasLoaded = true;
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            IsNotFound = true;
            HasLoaded = true;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Loading achievement {Id} failed", _id);
            await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task OpenParentAsync() => _parentId is { } parent ? navigator.OpenAsync($"achievements/{parent}") : Task.CompletedTask;

    [RelayCommand]
    private Task OpenEarnerAsync(MemberRow earner) =>
        earner.UserId is null ? Task.CompletedTask : navigator.OpenAsync(NativeRoutes.UserProfilePath(earner.UserId));
}
