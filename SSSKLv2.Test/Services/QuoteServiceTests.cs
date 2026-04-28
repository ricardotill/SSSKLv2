using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SSSKLv2.Data;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Test.Util;
using NSubstitute;
using SSSKLv2.Data.Constants;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Test.Services;

[TestClass]
public class QuoteServiceTests : RepositoryTest
{
    private QuoteService _sut = null!;
    private ApplicationDbContext _dbContext = null!;
    private IQuoteRepository _quoteRepository = null!;
    private IApplicationUserService _userService = null!;
    private INotificationService _notificationService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContext = new ApplicationDbContext(GetOptions());
        _quoteRepository = Substitute.For<IQuoteRepository>();
        _userService = Substitute.For<IApplicationUserService>();
        _notificationService = Substitute.For<INotificationService>();
        _sut = new QuoteService(_quoteRepository, _userService, _dbContext, _notificationService);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task GetQuotesAsync_ShouldReturnQuotesWithCorrectCounts()
    {
        // Arrange
        var userId = TestUser.Id;
        var quoteId = Guid.NewGuid();
        var quote = new Quote { Id = quoteId, Text = "Test Quote", CreatedById = userId };
        
        // Add quote to db to satisfy FKs
        _dbContext.Quote.Add(quote);
        await _dbContext.SaveChangesAsync();

        _userService.GetUserRoles(userId).Returns(new List<string> { Roles.User });

        _quoteRepository.GetAll(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IList<string>>(), Arg.Any<bool>())
            .Returns(new List<Quote> { quote });

        // Add a vote
        _dbContext.QuoteVote.Add(new QuoteVote { QuoteId = quoteId, UserId = userId });
        
        // Add a comment (reaction)
        _dbContext.Reaction.Add(new Reaction 
        { 
            TargetId = quoteId, 
            TargetType = ReactionTargetType.Quote, 
            UserId = userId, 
            Content = "Nice comment" 
        });
        
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetQuotesAsync(0, 10, userId);

        // Assert
        var dto = result.First();
        dto.VoteCount.Should().Be(1);
        dto.CommentsCount.Should().Be(1);
        dto.HasVoted.Should().BeTrue();
    }

    [TestMethod]
    public async Task ToggleVoteAsync_NewVote_ShouldAddVote()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var userId = TestUser.Id;
        var quote = new Quote { Id = quoteId, Text = "Vote Quote", CreatedById = userId };
        _dbContext.Quote.Add(quote);
        await _dbContext.SaveChangesAsync();

        // Act
        var hasVoted = await _sut.ToggleVoteAsync(quoteId, userId);

        // Assert
        hasVoted.Should().BeTrue();
        var vote = await _dbContext.QuoteVote.FirstOrDefaultAsync(v => v.QuoteId == quoteId && v.UserId == userId);
        vote.Should().NotBeNull();
    }

    [TestMethod]
    public async Task ToggleVoteAsync_ExistingVote_ShouldRemoveVote()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var userId = TestUser.Id;
        var quote = new Quote { Id = quoteId, Text = "Unvote Quote", CreatedById = userId };
        _dbContext.Quote.Add(quote);
        await _dbContext.SaveChangesAsync();

        _dbContext.QuoteVote.Add(new QuoteVote { QuoteId = quoteId, UserId = userId });
        await _dbContext.SaveChangesAsync();

        // Act
        var hasVoted = await _sut.ToggleVoteAsync(quoteId, userId);

        // Assert
        hasVoted.Should().BeFalse();
        var vote = await _dbContext.QuoteVote.FirstOrDefaultAsync(v => v.QuoteId == quoteId && v.UserId == userId);
        vote.Should().BeNull();
    }

    [TestMethod]
    public async Task GetQuoteByIdAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var quote = new Quote { Id = quoteId, Text = "Single Quote", CreatedById = TestUser.Id };
        
        _dbContext.Quote.Add(quote);
        await _dbContext.SaveChangesAsync();

        _quoteRepository.GetById(quoteId).Returns(quote);

        _dbContext.QuoteVote.Add(new QuoteVote { QuoteId = quoteId, UserId = TestUser.Id });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetQuoteByIdAsync(quoteId, TestUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.VoteCount.Should().Be(1);
        result.HasVoted.Should().BeTrue();
    }
}
