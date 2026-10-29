using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using SSSKLv2.Data;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;
using NSubstitute;
using SSSKLv2.Events;
using SSSKLv2.Data.Constants;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Test.Services;

[TestClass]
public class QuoteServiceTests
{
    private const string UserId = "user-1";

    private QuoteService _sut = null!;
    private IQuoteRepository _quoteRepository = null!;
    private IApplicationUserService _userService = null!;
    private IReactionRepository _reactionRepository = null!;
    private IRoleRepository _roleRepository = null!;
    private IGlobalSettingRepository _globalSettingRepository = null!;
    private INotificationService _notificationService = null!;
    private IDomainEventDispatcher _mockEventDispatcher = null!;
    private IUserStatRepository _mockUserStatRepository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _quoteRepository = Substitute.For<IQuoteRepository>();
        _userService = Substitute.For<IApplicationUserService>();
        _reactionRepository = Substitute.For<IReactionRepository>();
        _roleRepository = Substitute.For<IRoleRepository>();
        _globalSettingRepository = Substitute.For<IGlobalSettingRepository>();
        _notificationService = Substitute.For<INotificationService>();
        _mockEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        _mockUserStatRepository = Substitute.For<IUserStatRepository>();
        _mockUserStatRepository.RecalculateByUserId(Arg.Any<string>()).Returns(call => new UserStat { UserId = call.Arg<string>() });
        _sut = new QuoteService(_quoteRepository, _userService, _reactionRepository, _roleRepository, _globalSettingRepository, _notificationService, _mockEventDispatcher, _mockUserStatRepository);
    }

    [TestMethod]
    public async Task GetQuotesAsync_ShouldReturnQuotesWithCorrectCounts()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new Quote { Id = quoteId, Text = "Test Quote", CreatedById = UserId };

        _userService.GetUserRoles(UserId).Returns(new List<string> { Roles.User });
        _quoteRepository.GetAll(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IList<string>>(), Arg.Any<bool>())
            .Returns((new List<Quote> { quote }, 1));
        _quoteRepository.GetVoteSummaries(Arg.Any<IEnumerable<Guid>>(), UserId)
            .Returns(new Dictionary<Guid, QuoteVoteSummary> { [quoteId] = new(1, true) });
        _reactionRepository.CountByTargets(ReactionTargetType.Quote, Arg.Any<IEnumerable<Guid>>())
            .Returns(new Dictionary<Guid, int> { [quoteId] = 1 });

        // Act
        var result = await _sut.GetQuotesAsync(0, 10, UserId);

        // Assert
        var dto = result.Items.First();
        dto.VoteCount.Should().Be(1);
        dto.CommentsCount.Should().Be(1);
        dto.HasVoted.Should().BeTrue();
    }

    [TestMethod]
    public async Task ToggleVoteAsync_NewVote_ShouldAddVote()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        _quoteRepository.GetVote(quoteId, UserId).Returns((QuoteVote?)null);

        // Act
        var hasVoted = await _sut.ToggleVoteAsync(quoteId, UserId);

        // Assert
        hasVoted.Should().BeTrue();
        await _quoteRepository.Received(1).AddVote(Arg.Is<QuoteVote>(v => v.QuoteId == quoteId && v.UserId == UserId));
        await _mockEventDispatcher.Received(1).DispatchAsync(Arg.Any<QuoteVotedEvent>());
    }

    [TestMethod]
    public async Task ToggleVoteAsync_ExistingVote_ShouldRemoveVoteAndRecalculateStats()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var vote = new QuoteVote { QuoteId = quoteId, UserId = UserId };
        _quoteRepository.GetVote(quoteId, UserId).Returns(vote);
        _quoteRepository.GetAuthorUserIds(quoteId).Returns(new List<string> { "author-1" });

        // Act
        var hasVoted = await _sut.ToggleVoteAsync(quoteId, UserId);

        // Assert
        hasVoted.Should().BeFalse();
        await _quoteRepository.Received(1).DeleteVote(vote);
        await _mockUserStatRepository.Received(1).RecalculateByUserId("author-1");
        await _mockUserStatRepository.Received(1).RecalculateByUserId(UserId);
    }

    [TestMethod]
    public async Task GetQuoteByIdAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new Quote { Id = quoteId, Text = "Single Quote", CreatedById = UserId };

        _quoteRepository.GetById(quoteId).Returns(quote);
        _quoteRepository.GetVoteSummaries(Arg.Any<IEnumerable<Guid>>(), UserId)
            .Returns(new Dictionary<Guid, QuoteVoteSummary> { [quoteId] = new(1, true) });
        _reactionRepository.CountByTarget(ReactionTargetType.Quote, quoteId).Returns(2);

        // Act
        var result = await _sut.GetQuoteByIdAsync(quoteId, UserId);

        // Assert
        result.Should().NotBeNull();
        result!.VoteCount.Should().Be(1);
        result.HasVoted.Should().BeTrue();
        result.CommentsCount.Should().Be(2);
    }

    [TestMethod]
    public async Task GetQuotesAsync_WhenFeatureRestrictedAndUserLacksRole_ShouldThrow()
    {
        // Arrange
        _globalSettingRepository.GetByKey("QuotesFeatureAllowedRoles")
            .Returns(new GlobalSetting { Key = "QuotesFeatureAllowedRoles", Value = "Leiding" });
        _userService.GetUserRoles(UserId).Returns(new List<string> { Roles.User });

        // Act
        var act = () => _sut.GetQuotesAsync(0, 10, UserId);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
