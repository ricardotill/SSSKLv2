using System.Collections.ObjectModel;
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

/// <summary>Who said a quote: a member (tappable, with avatar) or a free-text name.</summary>
public sealed record QuoteAuthorItem(string Name, string? UserId, string? PictureUrl)
{
    public string Initials { get; } = UserItemViewModel.InitialsOf(Name);
    public bool IsMember => !string.IsNullOrEmpty(UserId);
}

/// <summary>A quote card. The vote changes in place (optimistically).</summary>
public sealed partial class QuoteRow : ObservableObject
{
    public QuoteRow(QuoteItem quote, AppEndpoints endpoints)
    {
        Id = quote.Id;
        Text = $"“{quote.Text}”";
        DateText = Display.Date(quote.DateSaid);
        Authors = quote.Authors
            .Select(a => new QuoteAuthorItem(a.DisplayName, a.ApplicationUserId, endpoints.ResolveApiUrl(a.ApplicationUser?.ProfilePictureUrl)))
            .ToList();
        AuthorsText = "— " + string.Join(" & ", Authors.Select(a => a.Name));
        CommentsText = Texts.CommentCount(quote.CommentsCount);
        CommentsCount = quote.CommentsCount;
        VoteCount = quote.VoteCount;
        HasVoted = quote.HasVoted;
        CreatedById = quote.CreatedBy?.Id;
    }

    public Guid Id { get; }
    public string Text { get; }
    public string DateText { get; }
    public IReadOnlyList<QuoteAuthorItem> Authors { get; }
    public string AuthorsText { get; }
    public int CommentsCount { get; }
    public string CommentsText { get; }
    public string? CreatedById { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoteText), nameof(AccessibleText))]
    public partial int VoteCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccessibleText), nameof(HasNotVoted))]
    public partial bool HasVoted { get; set; }

    public bool HasNotVoted => !HasVoted;

    public string VoteText => VoteCount.ToString(Display.Dutch);

    public string AccessibleText => $"{Text} {AuthorsText}, {DateText}, {Texts.Votes(VoteCount)}, {CommentsText}";

    /// <summary>Flips the vote locally; returns the old state to undo it.</summary>
    public (int Count, bool Voted) Flip()
    {
        var before = (VoteCount, HasVoted);
        HasVoted = !HasVoted;
        VoteCount = Math.Max(0, VoteCount + (HasVoted ? 1 : -1));
        return before;
    }

    public void Restore((int Count, bool Voted) state)
    {
        VoteCount = state.Count;
        HasVoted = state.Voted;
    }

    /// <summary>Like the SPA: admins, whoever added it, and its authors may edit or delete a quote.</summary>
    public bool CanEditBy(CurrentUser? user) =>
        user is not null && (user.IsInRole(RoleNames.Admin) || CreatedById == user.Id || Authors.Any(a => a.UserId == user.Id));
}

/// <summary>Votes on a quote card or detail: optimistic, undone when the API call fails.</summary>
internal static class QuoteVoting
{
    public static async Task ToggleAsync(QuoteRow row, ISssklApi api, IUserDialogs dialogs, ILogger logger)
    {
        var before = row.Flip();
        try
        {
            var voted = await api.ToggleQuoteVoteAsync(row.Id);
            if (voted != row.HasVoted)
            {
                // The server knows better (e.g. a vote from another device): follow it.
                row.Restore(before);
                if (voted != before.Voted)
                {
                    row.Flip();
                }
            }
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Voting on quote {Id} failed", row.Id);
            row.Restore(before);
            await dialogs.AlertAsync(Texts.Error, Texts.VoteFailed);
        }
    }
}

/// <summary>
/// Quotes (SPA quotes), newest first, a page at a time, with a vote (❤️) per card. Adding one opens the
/// SPA's form (quotes?new) in the WebView, for those the QuotesFeatureAllowedRoles setting allows.
/// </summary>
public sealed partial class QuotesViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    ILogger<QuotesViewModel> logger) : PagedListViewModel<QuoteRow>(dialogs, logger)
{
    public const string AllowedRolesSetting = "QuotesFeatureAllowedRoles";

    [ObservableProperty]
    public partial bool CanCreate { get; set; }

    protected override async Task<(IReadOnlyList<QuoteRow> Items, int Total)> FetchAsync(int skip, int take)
    {
        var page = await api.GetQuotesAsync(skip, take);
        return (page.Items.Select(q => new QuoteRow(q, endpoints)).ToList(), page.TotalCount);
    }

    protected override object KeyOf(QuoteRow item) => item.Id;

    protected override async Task OnLoadedAsync()
    {
        try
        {
            var user = currentUser.User ?? await currentUser.RefreshAsync();
            if (user.IsInRole(RoleNames.Admin))
            {
                CanCreate = true;
                return;
            }
            // Like the SPA: open to everyone when the setting is empty.
            var setting = await api.GetGlobalSettingAsync(AllowedRolesSetting);
            var roles = (setting?.Value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            CanCreate = roles.Length == 0 || roles.Any(user.IsInRole);
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            Logger.LogWarning(ex, "Loading who may add quotes failed");
        }
    }

    [RelayCommand]
    private Task VoteAsync(QuoteRow row) => QuoteVoting.ToggleAsync(row, api, Dialogs, Logger);

    [RelayCommand]
    private Task OpenAsync(QuoteRow row) => navigator.OpenAsync($"quotes/{row.Id}");

    [RelayCommand]
    private Task NewQuoteAsync() => navigator.OpenWebAsync("quotes?new=1");
}

/// <summary>
/// One quote (SPA quotes/{id}): the text, who said it (members open their profile), the vote, and the
/// comments. Editing opens the SPA's form (quotes?edit=) in the WebView; deleting asks first.
/// </summary>
public sealed partial class QuoteDetailViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    CommentsViewModel comments,
    ILogger<QuoteDetailViewModel> logger) : ObservableObject
{
    private Guid _id;

    public CommentsViewModel Comments => comments;
    public ObservableCollection<QuoteAuthorItem> Authors { get; } = [];

    [ObservableProperty]
    public partial QuoteRow? Quote { get; set; }

    [ObservableProperty]
    public partial bool CanEdit { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFound))]
    public partial bool HasLoaded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFound))]
    public partial bool IsNotFound { get; set; }

    public bool IsFound => HasLoaded && !IsNotFound;

    public void Initialize(Guid id)
    {
        _id = id;
        comments.Initialize(id, ReactionTarget.Quote);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var me = currentUser.User ?? await currentUser.RefreshAsync();
            var commentsTask = comments.LoadAsync();
            var quote = new QuoteRow(await api.GetQuoteAsync(_id), endpoints);
            Quote = quote;
            CanEdit = quote.CanEditBy(me);
            Authors.Clear();
            foreach (var author in quote.Authors)
            {
                Authors.Add(author);
            }
            HasLoaded = true;
            await commentsTask;
        }
        catch (ApiException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            IsNotFound = true;
            HasLoaded = true;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(ex, "Loading quote {Id} failed", _id);
            await dialogs.AlertAsync(Texts.Error, Texts.LoadFailed);
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task VoteAsync() => Quote is { } quote ? QuoteVoting.ToggleAsync(quote, api, dialogs, logger) : Task.CompletedTask;

    [RelayCommand]
    private Task OpenAuthorAsync(QuoteAuthorItem author) =>
        author.UserId is null ? Task.CompletedTask : navigator.OpenAsync(NativeRoutes.UserProfilePath(author.UserId));

    [RelayCommand]
    private Task EditAsync() => navigator.OpenWebAsync($"quotes?edit={_id}");

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!await dialogs.ConfirmAsync(Texts.DeleteQuote, Texts.ConfirmDeleteQuote, Texts.Delete, Texts.Cancel))
        {
            return;
        }
        try
        {
            await api.DeleteQuoteAsync(_id);
            await dialogs.ToastAsync(Texts.QuoteDeleted);
            await navigator.CloseAsync();
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Deleting quote {Id} failed", _id);
            await dialogs.AlertAsync(Texts.Error, Texts.DeleteFailed);
        }
    }
}
