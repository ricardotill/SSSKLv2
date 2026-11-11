using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
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

/// <summary>
/// One event (SPA events/{id}): image, times, location (opens Google Maps), description, "Ben je erbij?",
/// attendees and comments. The creator and admins can change the image, delete it, or edit it in the
/// SPA's form (events/{id}/edit) in the WebView.
/// </summary>
public sealed partial class EventDetailViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    IPhotoPicker photoPicker,
    IShareService share,
    IExternalLauncher launcher,
    AppEndpoints endpoints,
    TimeProvider time,
    CommentsViewModel comments,
    ILogger<EventDetailViewModel> logger) : ObservableObject
{
    /// <summary>Event images above this size are rejected client-side, like profile pictures.</summary>
    public const int MaxImageBytes = 5 * 1024 * 1024;

    private Guid _id;
    private Uri? _mapsUrl;

    public CommentsViewModel Comments => comments;
    public ObservableCollection<MemberRow> Accepted { get; } = [];
    public ObservableCollection<MemberRow> Declined { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    public partial string? ImageUrl { get; set; }

    [ObservableProperty]
    public partial string StartText { get; set; } = "";

    [ObservableProperty]
    public partial string EndText { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLocation))]
    public partial string? LocationName { get; set; }

    [ObservableProperty]
    public partial bool CanOpenMaps { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRequiredRoles))]
    public partial string? RequiredRolesText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDescription))]
    public partial string? DescriptionHtml { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAccepted), nameof(IsDeclined))]
    public partial string? UserResponse { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsRsvpNoRole))]
    public partial bool CanRsvp { get; set; }

    [ObservableProperty]
    public partial bool CanEdit { get; set; }

    [ObservableProperty]
    public partial bool IsPast { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RsvpCommand))]
    public partial bool IsResponding { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string AttendeesText { get; set; } = "";

    [ObservableProperty]
    public partial string DeclinedText { get; set; } = "";

    [ObservableProperty]
    public partial bool HasNoAttendees { get; set; }

    [ObservableProperty]
    public partial bool HasDeclined { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFound), nameof(ShowsRsvpNoRole))]
    public partial bool HasLoaded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFound))]
    public partial bool IsNotFound { get; set; }

    public bool HasImage => ImageUrl is not null;
    public bool HasLocation => LocationName is not null;
    public bool HasRequiredRoles => RequiredRolesText is not null;
    public bool HasDescription => DescriptionHtml is not null;
    public bool IsAccepted => UserResponse == EventResponse.Accepted;
    public bool IsDeclined => UserResponse == EventResponse.Declined;
    public bool IsFound => HasLoaded && !IsNotFound;
    public bool ShowsRsvpNoRole => IsFound && !CanRsvp;

    public void Initialize(Guid id)
    {
        _id = id;
        comments.Initialize(id, ReactionTarget.Event);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var me = currentUser.User ?? await currentUser.RefreshAsync();
            var commentsTask = comments.LoadAsync();
            var item = await api.GetEventAsync(_id);
            Apply(item, me);
            HasLoaded = true;
            await commentsTask;
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            IsNotFound = true;
            HasLoaded = true;
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Loading event {Id} failed", _id);
            await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    private bool CanRespond() => !IsResponding;

    [RelayCommand(CanExecute = nameof(CanRespond))]
    private async Task RsvpAsync(string status)
    {
        IsResponding = true;
        try
        {
            await api.RespondToEventAsync(_id, status);
            await dialogs.ToastAsync(status == EventResponse.Accepted ? Texts.RsvpAccepted : Texts.RsvpDeclined);
            Apply(await api.GetEventAsync(_id), currentUser.User);
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Responding to event {Id} failed", _id);
            await dialogs.AlertAsync(Texts.Error, Texts.RsvpFailed);
        }
        finally
        {
            IsResponding = false;
        }
    }

    [RelayCommand]
    private Task OpenMapsAsync() => _mapsUrl is null ? Task.CompletedTask : launcher.OpenAsync(_mapsUrl);

    [RelayCommand]
    private Task ShareAsync() => share.ShareAsync(Title, Texts.ShareEventText(Title), endpoints.WebUrl($"events/{_id}"));

    [RelayCommand]
    private Task EditAsync() => navigator.OpenWebAsync($"events/{_id}/edit");

    [RelayCommand]
    private async Task ChangeImageAsync()
    {
        PickedPhoto? photo;
        try
        {
            photo = await photoPicker.PickAsync();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            // Permission or decoding errors from the platform picker (Core can't name MAUI's exception types).
            logger.LogWarning(ex, "Picking an event image failed");
            await dialogs.AlertAsync(Texts.Error, Texts.ImageUpdateFailed);
            return;
        }
        if (photo is null)
        {
            return;
        }
        if (photo.Content.Length > MaxImageBytes)
        {
            await dialogs.AlertAsync(Texts.Error, Texts.ImageUpdateFailed);
            return;
        }

        IsBusy = true;
        try
        {
            await api.UpdateEventImageAsync(_id, new Base64FileUpload(photo.FileName, photo.ContentType, Convert.ToBase64String(photo.Content)));
            await dialogs.ToastAsync(Texts.ImageUpdated);
            var item = await api.GetEventAsync(_id);
            Apply(item, currentUser.User);
            // Same URL after a new upload: bust the image cache.
            if (ImageUrl is not null)
            {
                ImageUrl = $"{ImageUrl}{(ImageUrl.Contains('?') ? '&' : '?')}v={time.GetUtcNow().Ticks}";
            }
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Uploading the image of event {Id} failed", _id);
            await dialogs.AlertAsync(Texts.Error, Texts.ImageUpdateFailed);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!await dialogs.ConfirmAsync(Texts.DeleteEvent, Texts.ConfirmDeleteEvent, Texts.Delete, Texts.Cancel))
        {
            return;
        }
        IsBusy = true;
        try
        {
            await api.DeleteEventAsync(_id);
            await dialogs.ToastAsync(Texts.EventDeleted);
            await navigator.CloseAsync();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Deleting event {Id} failed", _id);
            await dialogs.AlertAsync(Texts.Error, Texts.DeleteFailed);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task OpenMemberAsync(MemberRow member) =>
        member.UserId is null ? Task.CompletedTask : navigator.OpenAsync(NativeRoutes.UserProfilePath(member.UserId));

    private void Apply(EventItem item, CurrentUser? me)
    {
        Title = item.Title;
        ImageUrl = endpoints.ResolveApiUrl(item.ImageUrl);
        StartText = LongDate(item.StartDateTime);
        EndText = LongDate(item.EndDateTime);
        IsPast = EventTime.IsPast(item.EndDateTime, time.GetUtcNow().UtcDateTime);
        LocationName = string.IsNullOrWhiteSpace(item.LocationName) ? null : item.LocationName;
        _mapsUrl = MapsUrl(item);
        CanOpenMaps = _mapsUrl is not null;
        RequiredRolesText = item.RequiredRoles.Count == 0 ? null : string.Join(", ", item.RequiredRoles);
        DescriptionHtml = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description;
        UserResponse = item.UserResponse;

        var isAdmin = me?.IsInRole(RoleNames.Admin) ?? false;
        var isMember = isAdmin || (me?.IsInRole(RoleNames.User) ?? false);
        // Like the SPA, plus the API's own rule: only members (User, Admin) can respond, edit or delete.
        CanRsvp = isMember && (isAdmin || item.RequiredRoles.Count == 0 || item.RequiredRoles.Any(r => me!.IsInRole(r)));
        CanEdit = isMember && (isAdmin || item.CreatorId == me?.Id);

        Fill(Accepted, item.AcceptedUsers, me?.Id);
        Fill(Declined, item.DeclinedUsers, me?.Id);
        AttendeesText = $"{Texts.Attendees} ({item.AcceptedUsers.Count})";
        DeclinedText = $"{Texts.Declined} ({item.DeclinedUsers.Count})";
        HasNoAttendees = item.AcceptedUsers.Count == 0;
        HasDeclined = item.DeclinedUsers.Count > 0;
    }

    private void Fill(ObservableCollection<MemberRow> rows, IReadOnlyList<EventMember> members, string? me)
    {
        rows.Clear();
        foreach (var member in members)
        {
            rows.Add(new MemberRow(member.UserId, member.UserName, endpoints.ResolveApiUrl(member.ProfilePictureUrl), "", IsSelf: member.UserId == me));
        }
    }

    /// <summary>"woensdag 11 november 2026, 20:00", like the SPA.</summary>
    private static string LongDate(DateTime utc) => Display.ToLocal(utc).ToString("dddd d MMMM yyyy, HH:mm", Display.Dutch);

    /// <summary>Google Maps at the coordinates (like the SPA), or a search for the location's name.</summary>
    public static Uri? MapsUrl(EventItem item)
    {
        if (Coordinate(item.Latitude) is { } lat && Coordinate(item.Longitude) is { } lng)
        {
            // Six decimals (about 10 cm), which also drops the floating-point noise of the scaling below.
            return new Uri($"https://www.google.com/maps/search/?api=1&query={lat.ToString("0.######", CultureInfo.InvariantCulture)},{lng.ToString("0.######", CultureInfo.InvariantCulture)}");
        }
        return string.IsNullOrWhiteSpace(item.LocationName)
            ? null
            : new Uri($"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(item.LocationName)}");
    }

    /// <summary>
    /// Like the SPA's sanitizeCoordinate: coordinates mangled by locale issues (a lost decimal point) are
    /// scaled back into range.
    /// </summary>
    private static double? Coordinate(double? value)
    {
        if (value is not { } coordinate || double.IsNaN(coordinate))
        {
            return null;
        }
        while (Math.Abs(coordinate) > 180)
        {
            coordinate /= 10;
        }
        return coordinate;
    }

    private static bool IsExpected(Exception ex) =>
        ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException;
}
