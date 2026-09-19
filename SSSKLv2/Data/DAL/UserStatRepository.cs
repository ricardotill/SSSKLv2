using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class UserStatRepository(ApplicationDbContext context, IProductUserStatRepository productUserStatRepository) : IUserStatRepository
{
    public async Task<UserStat> GetByUserId(string userId)
    {
        return await context.UserStats.FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<UserStat> GetOrCreateByUserId(string userId)
    {
        var stats = await GetByUserId(userId);
        if (stats == null)
        {
            stats = new UserStat { UserId = userId };
            context.UserStats.Add(stats);
            await context.SaveChangesAsync();
        }
        return stats;
    }

    public async Task<UserStat> RecalculateByUserId(string userId)
    {
        var stats = await GetOrCreateByUserId(userId);
        var orders = await context.Order
            .Where(order => order.User.Id == userId)
            .OrderBy(order => order.CreatedOn)
            .ToListAsync();
        var topUps = await context.TopUp
            .Where(topUp => topUp.User.Id == userId)
            .OrderBy(topUp => topUp.CreatedOn)
            .ToListAsync();
        var quoteDates = await context.Quote
            .Where(quote => quote.CreatedById == userId)
            .Select(quote => quote.CreatedOn)
            .ToListAsync();
        var voteDates = await context.QuoteVote
            .Where(vote => vote.UserId == userId)
            .Select(vote => vote.CreatedOn)
            .ToListAsync();
        var reactionDates = await context.Reaction
            .Where(reaction => reaction.UserId == userId)
            .Select(reaction => reaction.CreatedOn)
            .ToListAsync();

        var authoredQuoteIds = await context.QuoteAuthor
            .Where(author => author.ApplicationUserId == userId)
            .Select(author => author.QuoteId)
            .ToListAsync();
        var receivedVoteCount = authoredQuoteIds.Count == 0
            ? 0
            : await context.QuoteVote.CountAsync(vote => authoredQuoteIds.Contains(vote.QuoteId));

        var orderDates = orders.Select(order => order.CreatedOn).ToList();
        var topUpDates = topUps.Select(topUp => topUp.CreatedOn).ToList();
        var activityDates = orderDates
            .Concat(topUpDates)
            .Concat(quoteDates)
            .Concat(voteDates)
            .Concat(reactionDates)
            .OrderBy(date => date)
            .ToList();

        stats.TotalSpent = orders.Sum(order => order.Paid);
        stats.TotalOrders = orders.Count;
        stats.TotalItemsBought = orders.Sum(order => order.Amount);
        stats.TotalTopUp = topUps.Sum(topUp => topUp.Saldo);
        stats.QuoteCount = quoteDates.Count;
        stats.QuoteVotesGiven = voteDates.Count;
        stats.QuoteVotesReceived = receivedVoteCount;
        stats.ReactionCount = reactionDates.Count;
        stats.MembershipStartDate = orderDates.FirstOrDefault();
        stats.LastOrderDate = orderDates.LastOrDefault();
        stats.LastTopUpDate = topUpDates.LastOrDefault();
        stats.MaxSingleTopUp = topUps.Count == 0 ? 0 : topUps.Max(topUp => topUp.Saldo);
        stats.MinMinutesBetweenOrders = GetMinimumIntervalMinutes(orderDates);
        stats.MinMinutesBetweenTopUp = GetMinimumIntervalMinutes(topUpDates);
        stats.MaxOrdersPerHour = GetMaximumRollingCount(orderDates, TimeSpan.FromHours(1));
        stats.LastActivityDate = activityDates.LastOrDefault();
        stats.CurrentStreak = GetCurrentStreak(activityDates);

        await context.SaveChangesAsync();

        await productUserStatRepository.RecalculateByUserId(userId);

        return stats;
    }

    public async Task<bool> TryMarkRecalculatedAtIfEligible(string userId, DateTime claimedAtUtc, TimeSpan minInterval)
    {
        await GetOrCreateByUserId(userId);

        var threshold = claimedAtUtc - minInterval;
        var rowsUpdated = await context.UserStats
            .Where(stats =>
                stats.UserId == userId &&
                (!stats.LastStatsRecalculatedAt.HasValue || stats.LastStatsRecalculatedAt.Value <= threshold))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(stats => stats.LastStatsRecalculatedAt, claimedAtUtc));

        return rowsUpdated > 0;
    }

    private static int GetMinimumIntervalMinutes(IReadOnlyList<DateTime> dates)
    {
        if (dates.Count < 2) return int.MaxValue;

        var minimum = int.MaxValue;
        for (var index = 1; index < dates.Count; index++)
        {
            var minutes = (int)(dates[index] - dates[index - 1]).TotalMinutes;
            if (minutes < minimum) minimum = minutes;
        }

        return minimum;
    }

    private static int GetMaximumRollingCount(IReadOnlyList<DateTime> dates, TimeSpan window)
    {
        var maximum = 0;
        var start = 0;
        for (var end = 0; end < dates.Count; end++)
        {
            while (dates[end] - dates[start] > window) start++;
            maximum = Math.Max(maximum, end - start + 1);
        }

        return maximum;
    }

    private static int GetCurrentStreak(IReadOnlyList<DateTime> dates)
    {
        if (dates.Count == 0) return 0;

        var streak = 1;
        for (var index = 1; index < dates.Count; index++)
        {
            var dayDifference = (dates[index].Date - dates[index - 1].Date).Days;
            if (dayDifference == 1) streak++;
            else if (dayDifference > 1) streak = 1;
        }

        return streak;
    }

    public async Task Update(UserStat userStat)
    {
        context.UserStats.Update(userStat);
        await context.SaveChangesAsync();
    }
}
