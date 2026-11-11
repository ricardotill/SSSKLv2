using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SSSKL.Mobile.Core.Api;
using SSSKL.Mobile.Core.Configuration;
using SSSKL.Mobile.Core.Localization;
using SSSKL.Mobile.Core.Models;
using SSSKL.Mobile.Core.Services;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class QuoteAndCommentViewModelTests
{
    private static readonly Guid QuoteId = Guid.NewGuid();

    private readonly ISssklApi _api = Substitute.For<ISssklApi>();
    private readonly IAppNavigator _navigator = Substitute.For<IAppNavigator>();
    private readonly IUserDialogs _dialogs = Substitute.For<IUserDialogs>();
    private readonly AppEndpoints _endpoints = new(new Uri("https://api.test"), new Uri("https://web.test"));
    private CurrentUserService _currentUser = null!;

    private async Task SignInAsync(string id = "me", params string[] roles)
    {
        _api.GetMeAsync(Arg.Any<CancellationToken>()).Returns(new CurrentUser { Id = id, FullName = "Ik", Roles = roles.Length == 0 ? [RoleNames.User] : roles });
        _currentUser = new CurrentUserService(_api);
        await _currentUser.RefreshAsync();
    }

    private static QuoteItem Quote(bool voted = false, int votes = 3) => new()
    {
        Id = QuoteId, Text = "Hoi", DateSaid = new DateTime(2026, 3, 1), VoteCount = votes, HasVoted = voted, CommentsCount = 2,
        CreatedBy = new UserSummary { Id = "creator" },
        Authors =
        [
            new QuoteAuthor { ApplicationUserId = "anna", ApplicationUser = new UserSummary { Id = "anna", FullName = "Anna" } },
            new QuoteAuthor { CustomName = "Opa" }
        ]
    };

    private CommentsViewModel Comments() =>
        new(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<CommentsViewModel>.Instance);

    [TestMethod]
    public void QuoteRow_FormatsTheCard()
    {
        // Act
        var row = new QuoteRow(Quote(), _endpoints);

        // Assert
        row.Text.Should().Be("“Hoi”");
        row.AuthorsText.Should().Be("— Anna & Opa");
        row.DateText.Should().Be("1 mrt 2026");
        row.Authors.Select(a => a.IsMember).Should().Equal(true, false);
    }

    [TestMethod]
    [DataRow("creator", false, true)]
    [DataRow("anna", false, true)]
    [DataRow("other", false, false)]
    [DataRow("other", true, true)]
    public void QuoteRow_CanEdit_LikeTheSpa(string userId, bool admin, bool expected) =>
        new QuoteRow(Quote(), _endpoints).CanEditBy(new CurrentUser { Id = userId, Roles = admin ? [RoleNames.Admin] : [RoleNames.User] })
            .Should().Be(expected);

    [TestMethod]
    public async Task Vote_IsOptimisticAndUndoneOnFailure()
    {
        // Arrange
        await SignInAsync();
        _api.GetQuotesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new Page<QuoteItem> { Items = [Quote()], TotalCount = 1 });
        var sut = new QuotesViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<QuotesViewModel>.Instance);
        await sut.LoadCommand.ExecuteAsync(null);
        var row = sut.Items[0];
        _api.ToggleQuoteVoteAsync(QuoteId, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await sut.VoteCommand.ExecuteAsync(row);

        // Assert
        row.HasVoted.Should().BeTrue();
        row.VoteCount.Should().Be(4);

        // Act: it fails the second time.
        _api.ToggleQuoteVoteAsync(QuoteId, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        await sut.VoteCommand.ExecuteAsync(row);

        // Assert
        row.HasVoted.Should().BeTrue();
        row.VoteCount.Should().Be(4);
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.VoteFailed);
    }

    [TestMethod]
    public async Task Vote_FollowsTheServer()
    {
        // Arrange: voted on another device meanwhile.
        await SignInAsync();
        var row = new QuoteRow(Quote(voted: false, votes: 3), _endpoints);
        _api.ToggleQuoteVoteAsync(QuoteId, Arg.Any<CancellationToken>()).Returns(false);
        var sut = new QuotesViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<QuotesViewModel>.Instance);

        // Act
        await sut.VoteCommand.ExecuteAsync(row);

        // Assert
        row.HasVoted.Should().BeFalse();
        row.VoteCount.Should().Be(3);
    }

    [TestMethod]
    [DataRow("", true)]
    [DataRow("Leiding, Bestuur", false)]
    [DataRow("User", true)]
    public async Task Quotes_CanCreate_FromTheSetting(string roles, bool expected)
    {
        // Arrange
        await SignInAsync();
        _api.GetQuotesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new Page<QuoteItem>());
        _api.GetGlobalSettingAsync(QuotesViewModel.AllowedRolesSetting, Arg.Any<CancellationToken>())
            .Returns(new GlobalSetting(QuotesViewModel.AllowedRolesSetting, roles, ""));
        var sut = new QuotesViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, NullLogger<QuotesViewModel>.Instance);

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        sut.CanCreate.Should().Be(expected);
        await sut.NewQuoteCommand.ExecuteAsync(null);
        await _navigator.Received(1).OpenWebAsync("quotes?new=1");
    }

    [TestMethod]
    public async Task QuoteDetail_EditAndDelete()
    {
        // Arrange
        await SignInAsync("creator");
        _api.GetQuoteAsync(QuoteId, Arg.Any<CancellationToken>()).Returns(Quote());
        _api.GetReactionsAsync(QuoteId, ReactionTarget.Quote, Arg.Any<CancellationToken>()).Returns([]);
        _dialogs.ConfirmAsync(default!, default!, default!, default!).ReturnsForAnyArgs(true);
        var sut = new QuoteDetailViewModel(_api, _currentUser, _navigator, _dialogs, _endpoints, Comments(), NullLogger<QuoteDetailViewModel>.Instance);
        sut.Initialize(QuoteId);
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.EditCommand.ExecuteAsync(null);
        await sut.OpenAuthorCommand.ExecuteAsync(sut.Authors[0]);
        await sut.DeleteCommand.ExecuteAsync(null);

        // Assert
        sut.CanEdit.Should().BeTrue();
        sut.Comments.QuickReactions.Should().NotContain(CommentsViewModel.Heart, "quotes have their own ❤️ vote");
        await _navigator.Received(1).OpenWebAsync($"quotes?edit={QuoteId}");
        await _navigator.Received(1).OpenAsync("users/anna");
        await _api.Received(1).DeleteQuoteAsync(QuoteId, Arg.Any<CancellationToken>());
        await _navigator.Received(1).CloseAsync();
    }

    private static Reaction Reaction(string user, string content, DateTime? at = null, params Reaction[] children) => new()
    {
        Id = Guid.NewGuid(), UserId = user, UserName = user, Content = content, CreatedOn = at ?? new DateTime(2026, 1, 1), Reactions = children
    };

    [TestMethod]
    public async Task Comments_SplitLikesFromReplies()
    {
        // Arrange
        await SignInAsync();
        var target = Guid.NewGuid();
        _api.GetReactionsAsync(target, ReactionTarget.Event, Arg.Any<CancellationToken>()).Returns(
        [
            Reaction("anna", "Wat leuk!", null,
                Reaction("me", CommentsViewModel.Like),
                Reaction("bert", CommentsViewModel.Like),
                Reaction("bert", CommentsViewModel.Heart),
                Reaction("me", "Inderdaad", new DateTime(2026, 1, 3)) with { TargetUserName = "anna" },
                Reaction("bert", "Zeker", new DateTime(2026, 1, 2))),
            Reaction("me", "Ik kom ook"),
        ]);
        var sut = Comments();
        sut.Initialize(target, ReactionTarget.Event);

        // Act
        await sut.LoadCommand.ExecuteAsync(null);

        // Assert
        var first = sut.Items[0];
        first.LikeCount.Should().Be(2);
        first.HasLiked.Should().BeTrue();
        first.HeartCount.Should().Be(1);
        first.HasHearted.Should().BeFalse();
        first.Replies.Select(r => r.Content).Should().Equal("Zeker", "Inderdaad");
        first.Replies[1].TargetText.Should().Be("  → @anna");
        first.CanDelete.Should().BeFalse();
        sut.Items[1].CanDelete.Should().BeTrue("it's yours");
        sut.HasNoComments.Should().BeFalse();
        sut.QuickReactions.Should().Contain(CommentsViewModel.Heart);
    }

    [TestMethod]
    public async Task Comments_PostAndReply()
    {
        // Arrange
        await SignInAsync();
        var target = Guid.NewGuid();
        var comment = Reaction("anna", "Hoi");
        _api.GetReactionsAsync(target, ReactionTarget.Quote, Arg.Any<CancellationToken>()).Returns([comment]);
        var sut = Comments();
        sut.Initialize(target, ReactionTarget.Quote);
        await sut.LoadCommand.ExecuteAsync(null);

        // Act: a comment
        sut.NewText = "  Grappig  ";
        await sut.PostCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).ToggleReactionAsync(new ToggleReaction(target, ReactionTarget.Quote, "Grappig"), Arg.Any<CancellationToken>());
        sut.NewText.Should().BeEmpty();

        // Act: a reply
        sut.StartReplyCommand.Execute(sut.Items[0]);
        sut.ReplyText.Should().Be(Texts.ReplyTo("anna"));
        sut.NewText = "Ja!";
        await sut.PostCommand.ExecuteAsync(null);

        // Assert
        await _api.Received(1).ToggleReactionAsync(new ToggleReaction(comment.Id, ReactionTarget.Reaction, "Ja!"), Arg.Any<CancellationToken>());
        sut.IsReplying.Should().BeFalse();
    }

    [TestMethod]
    public async Task Comments_PostFails_KeepsTheText()
    {
        // Arrange
        await SignInAsync();
        _api.GetReactionsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([]);
        _api.ToggleReactionAsync(Arg.Any<ToggleReaction>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));
        var sut = Comments();
        sut.Initialize(Guid.NewGuid(), ReactionTarget.Event);
        sut.NewText = "Hoi";

        // Act
        await sut.PostCommand.ExecuteAsync(null);

        // Assert
        sut.NewText.Should().Be("Hoi");
        await _dialogs.Received(1).AlertAsync(Texts.Error, Texts.CommentFailed);
    }

    [TestMethod]
    public async Task Comments_Delete_AsksFirst()
    {
        // Arrange
        await SignInAsync();
        var mine = Reaction("me", "Oeps");
        _api.GetReactionsAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns([mine]);
        _dialogs.ConfirmAsync(Texts.DeleteComment, Texts.ConfirmDeleteComment, Texts.Delete, Texts.Cancel).Returns(false, true);
        var sut = Comments();
        sut.Initialize(Guid.NewGuid(), ReactionTarget.Event);
        await sut.LoadCommand.ExecuteAsync(null);

        // Act
        await sut.DeleteCommand.ExecuteAsync(sut.Items[0]);
        await _api.DidNotReceiveWithAnyArgs().DeleteReactionAsync(default, default);
        await sut.DeleteCommand.ExecuteAsync(sut.Items[0]);

        // Assert
        await _api.Received(1).DeleteReactionAsync(mine.Id, Arg.Any<CancellationToken>());
    }
}
