using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Events;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Util;

namespace SSSKLv2.Services.Handlers;

public class AchievementEventHandler(
    IAchievementRepository achievementRepository,
    IUserStatRepository userStatRepository,
    IOrderRepository orderRepository,
    IAchievementService achievementService,
    ILogger<AchievementEventHandler> logger) : 
    IDomainEventHandler<OrderPlacedEvent>,
    IDomainEventHandler<TopUpEvent>,
    IDomainEventHandler<QuoteCreatedEvent>,
    IDomainEventHandler<QuoteVotedEvent>,
    IDomainEventHandler<ReactionAddedEvent>
{
    public async Task HandleAsync(OrderPlacedEvent domainEvent)
    {
        var order = domainEvent.Order;
        var stats = await userStatRepository.GetOrCreateByUserId(order.User.Id);
        
        // Update stats
        stats.TotalSpent += order.Paid;
        stats.TotalOrders += 1;
        stats.TotalItemsBought += order.Amount;
        
        if (!stats.MembershipStartDate.HasValue) 
            stats.MembershipStartDate = domainEvent.OccurredOn;

        if (stats.LastOrderDate.HasValue)
        {
            var diff = (int)(domainEvent.OccurredOn - stats.LastOrderDate.Value).TotalMinutes;
            if (diff < stats.MinMinutesBetweenOrders) stats.MinMinutesBetweenOrders = diff;
        }
        stats.LastOrderDate = domainEvent.OccurredOn;
        
        // Update MaxOrdersPerHour
        var hourAgo = domainEvent.OccurredOn.AddHours(-1);
        var orders = await orderRepository.GetPersonal(order.User.UserName);
        var recentOrdersCount = orders.Count(o => o.CreatedOn >= hourAgo);
        if (recentOrdersCount > stats.MaxOrdersPerHour) stats.MaxOrdersPerHour = recentOrdersCount;
        
        await userStatRepository.Update(stats);
        await UpdateStreak(stats, domainEvent.OccurredOn);
        
        await CheckAndAwardAchievements(order.User.Id, stats);
    }

    public async Task HandleAsync(TopUpEvent domainEvent)
    {
        var topUp = domainEvent.TopUp;
        var stats = await userStatRepository.GetOrCreateByUserId(topUp.User.Id);
        
        // Update stats
        stats.TotalTopUp += topUp.Saldo;
        if (topUp.Saldo > stats.MaxSingleTopUp) stats.MaxSingleTopUp = topUp.Saldo;

        if (stats.LastTopUpDate.HasValue)
        {
            var diff = (int)(domainEvent.OccurredOn - stats.LastTopUpDate.Value).TotalMinutes;
            if (diff < stats.MinMinutesBetweenTopUp) stats.MinMinutesBetweenTopUp = diff;
        }
        stats.LastTopUpDate = domainEvent.OccurredOn;

        stats.LastActivityDate = domainEvent.OccurredOn;
        
        await userStatRepository.Update(stats);
        await UpdateStreak(stats, domainEvent.OccurredOn);
        
        await CheckAndAwardAchievements(topUp.User.Id, stats);
    }

    public async Task HandleAsync(QuoteCreatedEvent domainEvent)
    {
        var quote = domainEvent.Quote;
        var stats = await userStatRepository.GetOrCreateByUserId(quote.CreatedById);
        
        stats.QuoteCount += 1;
        await UpdateStreak(stats, domainEvent.OccurredOn);
        
        await userStatRepository.Update(stats);
        await CheckAndAwardAchievements(quote.CreatedById, stats);
    }

    public async Task HandleAsync(QuoteVotedEvent domainEvent)
    {
        var vote = domainEvent.Vote;
        var stats = await userStatRepository.GetOrCreateByUserId(vote.UserId);
        
        stats.QuoteVotesGiven += 1;
        await UpdateStreak(stats, domainEvent.OccurredOn);
        await userStatRepository.Update(stats);

        // Also update QuoteVotesReceived for authors
        if (vote.Quote?.Authors != null)
        {
            foreach (var author in vote.Quote.Authors)
            {
                if (author.ApplicationUserId != null)
                {
                    var authorStats = await userStatRepository.GetOrCreateByUserId(author.ApplicationUserId);
                    authorStats.QuoteVotesReceived += 1;
                    await userStatRepository.Update(authorStats);
                    await CheckAndAwardAchievements(author.ApplicationUserId, authorStats);
                }
            }
        }
        
        await CheckAndAwardAchievements(vote.UserId, stats);
    }

    public async Task HandleAsync(ReactionAddedEvent domainEvent)
    {
        var reaction = domainEvent.Reaction;
        var stats = await userStatRepository.GetOrCreateByUserId(reaction.UserId);
        
        stats.ReactionCount += 1;
        await UpdateStreak(stats, domainEvent.OccurredOn);
        
        await userStatRepository.Update(stats);
        await CheckAndAwardAchievements(reaction.UserId, stats);
    }

    private async Task UpdateStreak(UserStat stats, DateTime occurredOn)
    {
        if (!stats.LastActivityDate.HasValue)
        {
            stats.CurrentStreak = 1;
        }
        else
        {
            var lastDate = stats.LastActivityDate.Value.Date;
            var currentDate = occurredOn.Date;
            var diff = (currentDate - lastDate).Days;

            if (diff == 1)
            {
                stats.CurrentStreak += 1;
            }
            else if (diff > 1)
            {
                stats.CurrentStreak = 1;
            }
        }

        stats.LastActivityDate = occurredOn;
        await userStatRepository.Update(stats);
    }

    private async Task CheckAndAwardAchievements(string userId, UserStat stats)
    {
        var uncompletedAchievements = await achievementRepository.GetUncompletedAchievementsForUser(stats.UserId);
        
        foreach (var achievement in uncompletedAchievements)
        {
            if (!achievement.AutoAchieve) continue;

            bool shouldAward = AchievementRulesUtil.CheckStatAchievement(achievement, stats);

            if (shouldAward)
            {
                logger.LogInformation("Awarding achievement {AchievementName} to user {UserId}", achievement.Name, userId);
                await achievementService.AwardAchievementToUser(userId, achievement.Id);
            }
        }
    }
}
