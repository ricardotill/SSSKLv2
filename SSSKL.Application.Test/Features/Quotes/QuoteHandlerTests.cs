using FluentAssertions;
using NSubstitute;
using SSSKL.Application.Features.Quotes;
using SSSKL.Application.Features.Quotes.Commands.CreateQuote;
using SSSKL.Application.Features.Quotes.Commands.DeleteQuote;
using SSSKL.Application.Features.Quotes.Commands.ToggleQuoteVote;
using SSSKL.Application.Features.Quotes.Commands.UpdateQuote;
using SSSKL.Application.Features.Quotes.Queries.GetQuoteById;
using SSSKL.Application.Features.Quotes.Queries.GetQuotes;
using SSSKL.Application.Shared;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Contracts.Events;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;
using SSSKL.Domain.Exceptions;

namespace SSSKL.Application.Test.Features.Quotes;

[TestClass]
public class QuoteHandlerTests
{
    private const string UserId = "user-1";

    private IQuoteRepository _quoteRepository = null!;
    private IUserRoleProvider _roleProvider = null!;
    private IReactionRepository _reactionRepository = null!;
    private IRoleRepository _roleRepository = null!;
    private IGlobalSettingRepository _globalSettingRepository = null!;
    private IUserNotifier _userNotifier = null!;
    private IDomainEventDispatcher _mockEventDispatcher = null!;
    private IUserStatRepository _mockUserStatRepository = null!;
    private QuoteAccess _quoteAccess = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _quoteRepository = Substitute.For<IQuoteRepository>();
        _roleProvider = Substitute.For<IUserRoleProvider>();
        _reactionRepository = Substitute.For<IReactionRepository>();
        _roleRepository = Substitute.For<IRoleRepository>();
        _globalSettingRepository = Substitute.For<IGlobalSettingRepository>();
        _userNotifier = Substitute.For<IUserNotifier>();
        _mockEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        _mockUserStatRepository = Substitute.For<IUserStatRepository>();
        _mockUserStatRepository.RecalculateByUserId(Arg.Any<string>()).Returns(call => new UserStat { UserId = call.Arg<string>() });
        _roleProvider.GetRoles(Arg.Any<string>()).Returns(new List<string> { Roles.User });
        _quoteAccess = new QuoteAccess(_globalSettingRepository, _roleProvider);
    }

    [TestMethod]
    public async Task GetQuotes_ShouldReturnQuotesWithCorrectCounts()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new Quote { Id = quoteId, Text = "Test Quote", CreatedById = UserId };

        _quoteRepository.GetAll(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IList<string>>(), Arg.Any<bool>())
            .Returns((new List<Quote> { quote }, 1));
        _quoteRepository.GetVoteSummaries(Arg.Any<IEnumerable<Guid>>(), UserId)
            .Returns(new Dictionary<Guid, QuoteVoteSummary> { [quoteId] = new(1, true) });
        _reactionRepository.CountByTargets(ReactionTargetType.Quote, Arg.Any<IEnumerable<Guid>>())
            .Returns(new Dictionary<Guid, int> { [quoteId] = 1 });
        var sut = new GetQuotesHandler(_quoteRepository, _reactionRepository, _roleProvider, _quoteAccess);

        // Act
        var result = await sut.Handle(new GetQuotesQuery(0, 10, UserId, null), CancellationToken.None);

        // Assert
        var dto = result.Items.First();
        dto.VoteCount.Should().Be(1);
        dto.CommentsCount.Should().Be(1);
        dto.HasVoted.Should().BeTrue();
        result.TotalCount.Should().Be(1);
    }

    [TestMethod]
    public async Task GetQuotes_WhenFeatureRestrictedAndUserLacksRole_ShouldThrow()
    {
        // Arrange
        _globalSettingRepository.GetByKey("QuotesFeatureAllowedRoles")
            .Returns(new GlobalSetting { Key = "QuotesFeatureAllowedRoles", Value = "Leiding" });
        var sut = new GetQuotesHandler(_quoteRepository, _reactionRepository, _roleProvider, _quoteAccess);

        // Act
        var act = () => sut.Handle(new GetQuotesQuery(0, 10, UserId, null), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task GetQuoteById_ShouldReturnCorrectCounts()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new Quote { Id = quoteId, Text = "Single Quote", CreatedById = UserId };

        _quoteRepository.GetById(quoteId).Returns(quote);
        _quoteRepository.GetVoteSummaries(Arg.Any<IEnumerable<Guid>>(), UserId)
            .Returns(new Dictionary<Guid, QuoteVoteSummary> { [quoteId] = new(1, true) });
        _reactionRepository.CountByTarget(ReactionTargetType.Quote, quoteId).Returns(2);
        var sut = new GetQuoteByIdHandler(_quoteRepository, _reactionRepository, _roleProvider, _quoteAccess);

        // Act
        var result = await sut.Handle(new GetQuoteByIdQuery(quoteId, UserId), CancellationToken.None);

        // Assert
        result.VoteCount.Should().Be(1);
        result.HasVoted.Should().BeTrue();
        result.CommentsCount.Should().Be(2);
    }

    [TestMethod]
    public async Task GetQuoteById_WhenRestrictedToOtherRole_ShouldThrowNotFound()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new Quote { Id = quoteId, Text = "Secret", CreatedById = "someone" };
        quote.VisibleToRoles.Add(new Microsoft.AspNetCore.Identity.IdentityRole("Leiding"));
        _quoteRepository.GetById(quoteId).Returns(quote);
        var sut = new GetQuoteByIdHandler(_quoteRepository, _reactionRepository, _roleProvider, _quoteAccess);

        // Act
        var act = () => sut.Handle(new GetQuoteByIdQuery(quoteId, UserId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task ToggleVote_NewVote_ShouldAddVote()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        _quoteRepository.GetVote(quoteId, UserId).Returns((QuoteVote?)null);
        var sut = new ToggleQuoteVoteHandler(_quoteRepository, _mockUserStatRepository, _mockEventDispatcher, _quoteAccess);

        // Act
        var hasVoted = await sut.Handle(new ToggleQuoteVoteCommand(quoteId, UserId), CancellationToken.None);

        // Assert
        hasVoted.Should().BeTrue();
        await _quoteRepository.Received(1).AddVote(Arg.Is<QuoteVote>(v => v.QuoteId == quoteId && v.UserId == UserId));
        await _mockEventDispatcher.Received(1).DispatchAsync(Arg.Any<QuoteVotedEvent>());
    }

    [TestMethod]
    public async Task ToggleVote_ExistingVote_ShouldRemoveVoteAndRecalculateStats()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var vote = new QuoteVote { QuoteId = quoteId, UserId = UserId };
        _quoteRepository.GetVote(quoteId, UserId).Returns(vote);
        _quoteRepository.GetAuthorUserIds(quoteId).Returns(new List<string> { "author-1" });
        var sut = new ToggleQuoteVoteHandler(_quoteRepository, _mockUserStatRepository, _mockEventDispatcher, _quoteAccess);

        // Act
        var hasVoted = await sut.Handle(new ToggleQuoteVoteCommand(quoteId, UserId), CancellationToken.None);

        // Assert
        hasVoted.Should().BeFalse();
        await _quoteRepository.Received(1).DeleteVote(vote);
        await _mockUserStatRepository.Received(1).RecalculateByUserId("author-1");
        await _mockUserStatRepository.Received(1).RecalculateByUserId(UserId);
    }

    [TestMethod]
    public async Task CreateQuote_WithNotification_NotifiesUsersInVisibleRoles()
    {
        // Arrange
        Quote? added = null;
        _quoteRepository.When(r => r.Add(Arg.Any<Quote>())).Do(ci => added = ci.Arg<Quote>());
        _quoteRepository.GetById(Arg.Any<Guid>()).Returns(_ => added);
        _roleRepository.GetUserIdsInRoles(Arg.Any<IEnumerable<string>>()).Returns(new List<string> { "leider-1" });
        var dto = new QuoteCreateDto
        {
            Text = "Hallo",
            VisibleToRoles = ["Leiding"],
            Authors = [new QuoteAuthorCreateDto { CustomName = "Jan" }],
            SendNotification = true
        };
        var sut = new CreateQuoteHandler(_quoteRepository, _roleRepository, _userNotifier, _mockEventDispatcher, _quoteAccess);

        // Act
        var result = await sut.Handle(new CreateQuoteCommand(dto, UserId), CancellationToken.None);

        // Assert
        result.Text.Should().Be("Hallo");
        await _mockEventDispatcher.Received(1).DispatchAsync(Arg.Any<QuoteCreatedEvent>());
        await _userNotifier.Received(1).NotifyManyAsync(
            Arg.Is<IEnumerable<string>?>(ids => ids != null && ids.Single() == "leider-1"),
            "Nieuwe Quote! 💬",
            "Jan: \"Hallo\"",
            "/quotes",
            true);
    }

    [TestMethod]
    public async Task CreateQuote_WithoutRoles_NotifiesEveryone()
    {
        // Arrange
        Quote? added = null;
        _quoteRepository.When(r => r.Add(Arg.Any<Quote>())).Do(ci => added = ci.Arg<Quote>());
        _quoteRepository.GetById(Arg.Any<Guid>()).Returns(_ => added);
        var sut = new CreateQuoteHandler(_quoteRepository, _roleRepository, _userNotifier, _mockEventDispatcher, _quoteAccess);

        // Act
        await sut.Handle(new CreateQuoteCommand(new QuoteCreateDto { Text = "Hoi", SendNotification = true }, UserId), CancellationToken.None);

        // Assert
        await _userNotifier.Received(1).NotifyManyAsync(null, Arg.Any<string>(), "Iemand: \"Hoi\"", "/quotes", true);
    }

    [TestMethod]
    public async Task UpdateQuote_ByUnrelatedUser_ShouldThrowUnauthorized()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        _quoteRepository.GetById(quoteId).Returns(new Quote { Id = quoteId, Text = "x", CreatedById = "someone-else" });
        var sut = new UpdateQuoteHandler(_quoteRepository, _roleRepository, _quoteAccess);

        // Act
        var act = () => sut.Handle(new UpdateQuoteCommand(quoteId, new QuoteUpdateDto { Text = "y" }, UserId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _quoteRepository.DidNotReceive().Update(Arg.Any<Quote>());
    }

    [TestMethod]
    public async Task UpdateQuote_ByAuthor_ShouldUpdateTextAndAuthors()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new Quote { Id = quoteId, Text = "x", CreatedById = "someone-else" };
        quote.Authors.Add(new QuoteAuthor { ApplicationUserId = UserId });
        _quoteRepository.GetById(quoteId).Returns(quote);
        var sut = new UpdateQuoteHandler(_quoteRepository, _roleRepository, _quoteAccess);

        // Act
        var result = await sut.Handle(new UpdateQuoteCommand(quoteId, new QuoteUpdateDto { Text = "y", Authors = [new QuoteAuthorCreateDto { CustomName = "Piet" }] }, UserId), CancellationToken.None);

        // Assert
        result.Text.Should().Be("y");
        result.Authors.Should().ContainSingle().Which.CustomName.Should().Be("Piet");
        await _quoteRepository.Received(1).Update(quote);
    }

    [TestMethod]
    public async Task DeleteQuote_WhenMissing_ShouldThrowNotFound()
    {
        // Arrange
        _quoteRepository.GetById(Arg.Any<Guid>()).Returns((Quote?)null);
        var sut = new DeleteQuoteHandler(_quoteRepository, _mockUserStatRepository, _quoteAccess);

        // Act
        var act = () => sut.Handle(new DeleteQuoteCommand(Guid.NewGuid(), UserId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task DeleteQuote_ByCreator_DeletesAndRecalculatesAuthorsAndCreator()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        _quoteRepository.GetById(quoteId).Returns(new Quote { Id = quoteId, Text = "x", CreatedById = UserId });
        _quoteRepository.GetAuthorUserIds(quoteId).Returns(new List<string> { "author-1" });
        var sut = new DeleteQuoteHandler(_quoteRepository, _mockUserStatRepository, _quoteAccess);

        // Act
        await sut.Handle(new DeleteQuoteCommand(quoteId, UserId), CancellationToken.None);

        // Assert
        await _quoteRepository.Received(1).Delete(quoteId);
        await _mockUserStatRepository.Received(1).RecalculateByUserId("author-1");
        await _mockUserStatRepository.Received(1).RecalculateByUserId(UserId);
    }
}
