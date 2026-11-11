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

/// <summary>
/// A comment or a reply to one. Top-level comments carry their replies and their 👍/❤️ counts.
/// </summary>
public sealed record CommentEntry(
    Guid Id,
    string UserId,
    string UserName,
    string? PictureUrl,
    string Content,
    string DateText,
    bool CanDelete,
    string? TargetUserName = null)
{
    public string Initials { get; } = UserItemViewModel.InitialsOf(UserName);
    public string Handle => "@" + UserName;
    public bool HasTargetUser => !string.IsNullOrEmpty(TargetUserName);
    /// <summary>"  → @anna" for a reply to someone; empty otherwise.</summary>
    public string TargetText => HasTargetUser ? $"  → @{TargetUserName}" : "";

    public IReadOnlyList<CommentEntry> Replies { get; init; } = [];
    public bool HasReplies => Replies.Count > 0;
    public int LikeCount { get; init; }
    public bool HasLiked { get; init; }
    public int HeartCount { get; init; }
    public bool HasHearted { get; init; }
    public string LikeText => LikeCount > 0 ? LikeCount.ToString(Display.Dutch) : "";
    public string HeartText => HeartCount > 0 ? HeartCount.ToString(Display.Dutch) : "";

    public string AccessibleText => $"{Handle}, {DateText}: {Content}";
}

/// <summary>
/// The comments under an event or quote (SPA shared/components/reaction-list): post a comment or a quick
/// emoji, give a comment a 👍 or ❤️, reply to a comment or a reply, and delete your own (admins: any).
/// Every change reloads the list, like the SPA.
/// </summary>
public sealed partial class CommentsViewModel(
    ISssklApi api,
    CurrentUserService currentUser,
    IAppNavigator navigator,
    IUserDialogs dialogs,
    AppEndpoints endpoints,
    ILogger<CommentsViewModel> logger) : ObservableObject
{
    public const string Like = "👍";
    public const string Heart = "❤️";
    private static readonly string[] AllQuickReactions = ["👍", "❤️", "😂", "🎉", "🔥"];

    private Guid _targetId;
    private string _targetType = ReactionTarget.Event;

    public ObservableCollection<CommentEntry> Items { get; } = [];

    /// <summary>Quick emoji under the input. Quotes have their own ❤️ vote, so it's left out there (like the SPA).</summary>
    public IReadOnlyList<string> QuickReactions { get; private set; } = AllQuickReactions;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PostCommand))]
    public partial string NewText { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReplying), nameof(ReplyText))]
    public partial CommentEntry? ReplyTarget { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PostCommand))]
    public partial bool IsPosting { get; set; }

    [ObservableProperty]
    public partial bool HasNoComments { get; set; }

    public bool IsReplying => ReplyTarget is not null;
    public string ReplyText => ReplyTarget is null ? "" : Texts.ReplyTo(ReplyTarget.UserName);
    public string Placeholder => Texts.CommentPlaceholder;

    public void Initialize(Guid targetId, string targetType)
    {
        _targetId = targetId;
        _targetType = targetType;
        QuickReactions = targetType == ReactionTarget.Quote ? AllQuickReactions.Where(r => r != Heart).ToArray() : AllQuickReactions;
        OnPropertyChanged(nameof(QuickReactions));
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            var me = currentUser.User;
            var canDeleteAny = me?.IsInRole(RoleNames.Admin) ?? false;
            var reactions = await api.GetReactionsAsync(_targetId, _targetType);
            Items.Clear();
            foreach (var reaction in reactions)
            {
                Items.Add(ToEntry(reaction, me?.Id, canDeleteAny));
            }
            HasNoComments = Items.Count == 0;
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            // The page around it still works; the list just stays as it was.
            logger.LogWarning(ex, "Loading reactions on {Type} {Id} failed", _targetType, _targetId);
        }
    }

    private bool CanPost() => !string.IsNullOrWhiteSpace(NewText) && !IsPosting;

    /// <summary>Posts the comment, or the reply when <see cref="ReplyTarget"/> is set.</summary>
    [RelayCommand(CanExecute = nameof(CanPost))]
    private async Task PostAsync()
    {
        var text = NewText.Trim();
        var target = ReplyTarget;
        if (await ToggleAsync(target?.Id ?? _targetId, target is null ? _targetType : ReactionTarget.Reaction, text))
        {
            NewText = "";
            ReplyTarget = null;
        }
    }

    [RelayCommand]
    private Task QuickAsync(string emoji) => ToggleAsync(_targetId, _targetType, emoji);

    [RelayCommand]
    private Task LikeAsync(CommentEntry comment) => ToggleAsync(comment.Id, ReactionTarget.Reaction, Like);

    [RelayCommand]
    private Task HeartAsync(CommentEntry comment) => ToggleAsync(comment.Id, ReactionTarget.Reaction, Heart);

    [RelayCommand]
    private void StartReply(CommentEntry comment) => ReplyTarget = ReferenceEquals(ReplyTarget, comment) ? null : comment;

    [RelayCommand]
    private void CancelReply() => ReplyTarget = null;

    [RelayCommand]
    private async Task DeleteAsync(CommentEntry comment)
    {
        if (!await dialogs.ConfirmAsync(Texts.DeleteComment, Texts.ConfirmDeleteComment, Texts.Delete, Texts.Cancel))
        {
            return;
        }
        try
        {
            await api.DeleteReactionAsync(comment.Id);
            if (ReplyTarget?.Id == comment.Id)
            {
                ReplyTarget = null;
            }
            await LoadAsync();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Deleting reaction {Id} failed", comment.Id);
            await dialogs.AlertAsync(Texts.Error, Texts.DeleteFailed);
        }
    }

    [RelayCommand]
    private Task OpenUserAsync(CommentEntry comment) =>
        string.IsNullOrEmpty(comment.UserId) ? Task.CompletedTask : navigator.OpenAsync(NativeRoutes.UserProfilePath(comment.UserId));

    private async Task<bool> ToggleAsync(Guid targetId, string targetType, string content)
    {
        IsPosting = true;
        try
        {
            await api.ToggleReactionAsync(new ToggleReaction(targetId, targetType, content));
            await LoadAsync();
            return true;
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            logger.LogWarning(ex, "Reacting on {Type} {Id} failed", targetType, targetId);
            await dialogs.AlertAsync(Texts.Error, ex is ApiException api && !string.IsNullOrWhiteSpace(api.Message) ? api.Message : Texts.CommentFailed);
            return false;
        }
        finally
        {
            IsPosting = false;
        }
    }

    private CommentEntry ToEntry(Reaction reaction, string? me, bool canDeleteAny)
    {
        var children = reaction.Reactions;
        // 👍 and ❤️ on a comment are likes; every other child is a reply in its thread.
        var replies = children
            .Where(c => c.Content is not (Like or Heart))
            .OrderBy(c => c.CreatedOn)
            .Select(c => Simple(c, me, canDeleteAny) with { TargetUserName = c.TargetUserName })
            .ToList();
        return Simple(reaction, me, canDeleteAny) with
        {
            Replies = replies,
            LikeCount = children.Count(c => c.Content == Like),
            HasLiked = children.Any(c => c.Content == Like && c.UserId == me),
            HeartCount = children.Count(c => c.Content == Heart),
            HasHearted = children.Any(c => c.Content == Heart && c.UserId == me)
        };
    }

    private CommentEntry Simple(Reaction reaction, string? me, bool canDeleteAny) => new(
        reaction.Id, reaction.UserId, reaction.UserName, endpoints.ResolveApiUrl(reaction.ProfilePictureUrl), reaction.Content,
        Display.DateTimeMedium(reaction.CreatedOn), canDeleteAny || (me is not null && reaction.UserId == me));

    private static bool IsExpected(Exception ex) =>
        ex is ApiException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException;
}
