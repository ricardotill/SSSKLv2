using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SSSKL.Application.Handlers;
using SSSKL.Contracts.Repositories;
using SSSKL.Contracts.Services;
using SSSKL.Domain.Entities;
using SSSKL.Domain.Events;

namespace SSSKL.Application.Test.Services;

[TestClass]
public class AchievementEventHandlerTests
{
    private IAchievementRepository _achievements = null!;
    private IUserStatRepository _userStats = null!;
    private IProductUserStatRepository _productStats = null!;
    private IOrderRepository _orders = null!;
    private IAchievementService _achievementService = null!;
    private AchievementEventHandler _sut = null!;

    [TestInitialize]
    public void Initialize()
    {
        _achievements = Substitute.For<IAchievementRepository>();
        _userStats = Substitute.For<IUserStatRepository>();
        _productStats = Substitute.For<IProductUserStatRepository>();
        _orders = Substitute.For<IOrderRepository>();
        _achievementService = Substitute.For<IAchievementService>();
        _achievements.GetUncompletedAchievementsForUser(Arg.Any<string>())
            .Returns(new List<Achievement>());
        _sut = new AchievementEventHandler(
            _achievements,
            _userStats,
            _productStats,
            _orders,
            _achievementService,
            Substitute.For<ILogger<AchievementEventHandler>>());
    }

    [TestMethod]
    public async Task HandleOrder_UpdatesUserAndProductStatsAndAwardsMatchingAchievement()
    {
        var user = new ApplicationUser { Id = "buyer", UserName = "buyer" };
        var product = new Product { Id = Guid.NewGuid(), Name = "Product" };
        var stats = new UserStat { UserId = user.Id, LastActivityDate = DateTime.UtcNow.AddDays(-1), LastOrderDate = DateTime.UtcNow.AddMinutes(-30), MinMinutesBetweenOrders = 60 };
        var productStats = new ProductUserStat { UserId = user.Id, ProductId = product.Id };
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Name = "Buy two",
            AutoAchieve = true,
            Action = Achievement.ActionOption.UserOrderAmountBought,
            ComparisonOperator = Achievement.ComparisonOperatorOption.GreaterThanOrEqual,
            ComparisonValue = 2
        };
        var order = new Order { User = user, Product = product, Amount = 2, Paid = 15, CreatedOn = DateTime.UtcNow };
        _userStats.GetOrCreateByUserId(user.Id).Returns(stats);
        _productStats.GetOrCreate(user.Id, product.Id).Returns(productStats);
        _orders.GetPersonal(user.UserName).Returns(new List<Order> { order });
        _achievements.GetUncompletedAchievementsForUser(user.Id).Returns(new List<Achievement> { achievement });

        await _sut.Handle(new OrderPlacedEvent(order), CancellationToken.None);

        stats.TotalSpent.Should().Be(15);
        stats.TotalOrders.Should().Be(1);
        stats.TotalItemsBought.Should().Be(2);
        stats.CurrentStreak.Should().Be(1);
        productStats.TotalAmount.Should().Be(2);
        productStats.TotalSpent.Should().Be(15);
        await _productStats.Received(1).Update(productStats);
        await _achievementService.Received(1).AwardAchievementToUser(user.Id, achievement.Id);
    }

    [TestMethod]
    public async Task HandleTopUp_UpdatesTotalsMaximumAndResetsStreakWhenGapExists()
    {
        var user = new ApplicationUser { Id = "topup-user" };
        var stats = new UserStat { UserId = user.Id, LastTopUpDate = DateTime.UtcNow.AddMinutes(-10), LastActivityDate = DateTime.UtcNow.AddDays(-3), CurrentStreak = 4, MinMinutesBetweenTopUp = 30 };
        var topUp = new TopUp { User = user, Saldo = 25 };
        _userStats.GetOrCreateByUserId(user.Id).Returns(stats);

        await _sut.Handle(new TopUpEvent(topUp), CancellationToken.None);

        stats.TotalTopUp.Should().Be(25);
        stats.MaxSingleTopUp.Should().Be(25);
        stats.MinMinutesBetweenTopUp.Should().Be(10);
        stats.CurrentStreak.Should().Be(1);
        await _userStats.Received(2).Update(stats);
    }

    [TestMethod]
    public async Task HandleQuoteCreatedAndReaction_IncrementTheirCounters()
    {
        var quoteStats = new UserStat { UserId = "quote-user" };
        var reactionStats = new UserStat { UserId = "reaction-user" };
        _userStats.GetOrCreateByUserId("quote-user").Returns(quoteStats);
        _userStats.GetOrCreateByUserId("reaction-user").Returns(reactionStats);

        await _sut.Handle(new QuoteCreatedEvent(new Quote { CreatedById = "quote-user" }), CancellationToken.None);
        await _sut.Handle(new ReactionAddedEvent(new Reaction { UserId = "reaction-user", Content = "like" }), CancellationToken.None);

        quoteStats.QuoteCount.Should().Be(1);
        reactionStats.ReactionCount.Should().Be(1);
        quoteStats.CurrentStreak.Should().Be(1);
        reactionStats.CurrentStreak.Should().Be(1);
    }

    [TestMethod]
    public async Task HandleQuoteVote_UpdatesVoterAndAuthors()
    {
        var voterStats = new UserStat { UserId = "voter" };
        var authorStats = new UserStat { UserId = "author" };
        _userStats.GetOrCreateByUserId("voter").Returns(voterStats);
        _userStats.GetOrCreateByUserId("author").Returns(authorStats);
        var vote = new QuoteVote
        {
            UserId = "voter",
            Quote = new Quote
            {
                Authors = new List<QuoteAuthor> { new() { ApplicationUserId = "author" } }
            }
        };

        await _sut.Handle(new QuoteVotedEvent(vote), CancellationToken.None);

        voterStats.QuoteVotesGiven.Should().Be(1);
        authorStats.QuoteVotesReceived.Should().Be(1);
        await _userStats.Received(2).Update(voterStats);
        await _userStats.Received(1).Update(authorStats);
    }
}