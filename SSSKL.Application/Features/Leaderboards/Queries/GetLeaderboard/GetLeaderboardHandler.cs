using MediatR;
using Microsoft.Extensions.Caching.Memory;
using SSSKL.Contracts.Dto;
using SSSKL.Contracts.Repositories;
using SSSKL.Domain.Entities;

namespace SSSKL.Application.Features.Leaderboards.Queries.GetLeaderboard;

internal sealed class GetLeaderboardHandler(
    IProductRepository productRepository,
    IProductUserStatRepository productUserStatRepository,
    IOrderRepository orderRepository,
    IApplicationUserRepository applicationUserRepository,
    IMemoryCache cache) : IRequestHandler<GetLeaderboardQuery, IReadOnlyList<LeaderboardEntryDto>>
{
    private static readonly TimeSpan LeaderboardCacheDuration = TimeSpan.FromSeconds(30);

    public async Task<IReadOnlyList<LeaderboardEntryDto>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = request.Period switch
        {
            LeaderboardPeriod.All => $"leaderboard:all:{request.ProductId}",
            LeaderboardPeriod.Monthly => $"leaderboard:monthly:{request.ProductId}",
            LeaderboardPeriod.TwelveHours => $"leaderboard:12hour:{request.ProductId}",
            _ => $"leaderboard:12hourlive:{request.ProductId}"
        };
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<LeaderboardEntryDto>? cached) && cached != null) return cached;

        // Throws NotFoundException for an unknown product
        var product = await productRepository.GetById(request.ProductId);

        var result = request.Period switch
        {
            LeaderboardPeriod.All => await BuildAllTime(product),
            LeaderboardPeriod.Monthly => await BuildMonthly(product),
            LeaderboardPeriod.TwelveHours => await BuildFromAggregates(
                await orderRepository.GetAmountAggregates(product.Id, DateTime.Now.AddHours(-12)), product),
            _ => await BuildLive(product)
        };

        cache.Set(cacheKey, result, LeaderboardCacheDuration);
        return result;
    }

    private async Task<IReadOnlyList<LeaderboardEntryDto>> BuildAllTime(Product product)
    {
        var stats = await productUserStatRepository.GetAllForProduct(product.Id);

        return DeterminePositions(stats
            .Where(s => s.TotalAmount > 0)
            .Select(s => new LeaderboardEntryDto
            {
                Amount = s.TotalAmount,
                FullName = $"{s.User.Name} {s.User.Surname.First()}",
                ProductName = product.Name,
                ProfilePictureUrl = s.User.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{s.User.ProfileImageId}" : null,
                UserId = s.UserId
            }));
    }

    private async Task<IReadOnlyList<LeaderboardEntryDto>> BuildMonthly(Product product)
    {
        var today = DateTime.Now.Date;
        var startDate = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var aggregates = await orderRepository.GetAmountAggregates(product.Id, startDate, startDate.AddMonths(1));
        return await BuildFromAggregates(aggregates, product);
    }

    private async Task<IReadOnlyList<LeaderboardEntryDto>> BuildLive(Product product)
    {
        var topUserIds = await applicationUserRepository.GetTopActiveUserIds(10);
        if (topUserIds.Count == 0) return [];

        var aggregates = await orderRepository.GetAmountAggregates(product.Id, DateTime.Now.AddHours(-12), userIds: topUserIds);
        return await BuildFromAggregates(aggregates, product);
    }

    private async Task<IReadOnlyList<LeaderboardEntryDto>> BuildFromAggregates(IList<OrderAggregate> aggregates, Product product)
    {
        var positive = aggregates.Where(a => a.Amount > 0).ToList();
        if (positive.Count == 0) return [];

        var users = await applicationUserRepository.GetByIds(positive.Select(a => a.UserId));
        var userMap = users.ToDictionary(u => u.Id);

        return DeterminePositions(positive
            .Where(a => userMap.ContainsKey(a.UserId))
            .Select(a =>
            {
                var u = userMap[a.UserId];
                return new LeaderboardEntryDto
                {
                    Amount = a.Amount > int.MaxValue ? int.MaxValue : (int)a.Amount,
                    FullName = $"{u.Name} {u.Surname.First()}",
                    ProductName = product.Name,
                    ProfilePictureUrl = u.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{u.ProfileImageId}" : null,
                    UserId = u.Id
                };
            }));
    }

    private static List<LeaderboardEntryDto> DeterminePositions(IEnumerable<LeaderboardEntryDto> leaderboard)
    {
        var list = leaderboard
            .OrderByDescending(x => x.Amount)
            .Take(18)
            .ToList();
        var place = 0;
        var lastAmount = 0;
        foreach (var entry in list)
        {
            if (entry.Amount != 0)
            {
                if (entry.Amount != lastAmount) place++;
                entry.Position = place;
                lastAmount = entry.Amount;
            }
            else entry.Position = 0;
        }

        return list;
    }
}
