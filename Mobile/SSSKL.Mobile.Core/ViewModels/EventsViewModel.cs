using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;

namespace SSSKL.Mobile.Core.ViewModels;

/// <summary>A small avatar (initials or picture) in a row of attendees.</summary>
public sealed record AvatarItem(string Initials, string? PictureUrl);

/// <summary>An event card: image, title, start, location, your response and the first attendees.</summary>
public sealed record EventRow(
    Guid Id, string Title, string? ImageUrl, string DateText, string? Location, bool IsPast, string? UserResponse,
    string AttendeesText, IReadOnlyList<AvatarItem> Attendees, int MoreAttendees)
{
    public const int AvatarCount = 3;

    public static EventRow From(EventItem item, DateTime nowUtc, AppEndpoints endpoints) => new(
        item.Id, item.Title, endpoints.ResolveApiUrl(item.ImageUrl), Display.DateTimeMedium(item.StartDateTime),
        string.IsNullOrWhiteSpace(item.LocationName) ? null : item.LocationName,
        EventTime.IsPast(item.EndDateTime, nowUtc), item.UserResponse,
        Texts.AttendeeCount(item.AcceptedUsers.Count),
        item.AcceptedUsers.Take(AvatarCount)
            .Select(u => new AvatarItem(UserItemViewModel.InitialsOf(u.UserName), endpoints.ResolveApiUrl(u.ProfilePictureUrl)))
            .ToList(),
        Math.Max(0, item.AcceptedUsers.Count - AvatarCount));

    public bool HasImage => ImageUrl is not null;
    public bool HasNoImage => ImageUrl is null;
    public bool HasLocation => Location is not null;
    public bool IsAccepted => UserResponse == EventResponse.Accepted;
    public bool IsDeclined => UserResponse == EventResponse.Declined;
    public bool HasMoreAttendees => MoreAttendees > 0;
    public string MoreAttendeesText => $"+{MoreAttendees}";

    public string AccessibleText => string.Join(", ", new[]
    {
        Title, DateText, Location, IsPast ? Texts.Past : null,
        IsAccepted ? Texts.Attending : IsDeclined ? Texts.NotAttending : null, AttendeesText
    }.Where(p => !string.IsNullOrEmpty(p)));
}

internal static class EventTime
{
    public static bool IsPast(DateTime endUtc, DateTime nowUtc) =>
        (endUtc.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(endUtc, DateTimeKind.Utc) : endUtc.ToUniversalTime()) < nowUtc;
}

/// <summary>
/// Events (SPA events): upcoming events, or all of them, a page at a time. Members (User, Admin) can add
/// one; the form is the SPA's (events/new) in the WebView.
/// </summary>
public sealed partial class EventsViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    TimeProvider time,
    ILogger<EventsViewModel> logger) : PagedListViewModel<EventRow>(dialogs, logger)
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowsAll))]
    public partial bool FutureOnly { get; set; } = true;

    public bool ShowsAll => !FutureOnly;

    /// <summary>Like the SPA's events/new route: User and Admin.</summary>
    public bool CanCreate => currentUser.User is { } user && (user.IsInRole(RoleNames.Admin) || user.IsInRole(RoleNames.User));

    protected override async Task<(IReadOnlyList<EventRow> Items, int Total)> FetchAsync(int skip, int take)
    {
        var page = await api.GetEventsAsync(skip, take, FutureOnly);
        var now = time.GetUtcNow().UtcDateTime;
        return (page.Items.Select(e => EventRow.From(e, now, endpoints)).ToList(), page.TotalCount);
    }

    protected override object KeyOf(EventRow item) => item.Id;

    protected override Task OnLoadedAsync()
    {
        OnPropertyChanged(nameof(CanCreate));
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task SetFutureOnlyAsync(bool futureOnly)
    {
        if (futureOnly == FutureOnly)
        {
            return Task.CompletedTask;
        }
        FutureOnly = futureOnly;
        return LoadAsync();
    }

    [RelayCommand]
    private Task OpenAsync(EventRow row) => navigator.OpenAsync($"events/{row.Id}");

    [RelayCommand]
    private Task NewEventAsync() => navigator.OpenWebAsync("events/new");
}
