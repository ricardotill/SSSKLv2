namespace SSSKL.Mobile.Core.ViewModels;

public sealed record OrderSummary(decimal PerUser, decimal Total);

/// <summary>
/// Cost preview, mirroring CreateOrderHandler (and the SPA's POS): with split each person pays every
/// product's price divided by the number of people, rounded up to the cent, times the amount.
/// </summary>
public static class OrderPricing
{
    public static OrderSummary? Calculate(IEnumerable<decimal> productPrices, int userCount, int amount, bool split)
    {
        var prices = productPrices.ToList();
        if (prices.Count == 0 || userCount == 0)
        {
            return null;
        }
        var doSplit = split && userCount > 1;
        var perUserCents = prices.Sum(price =>
        {
            var cents = Math.Round(price * 100m, MidpointRounding.AwayFromZero);
            return doSplit ? Math.Ceiling(cents / userCount) : cents;
        }) * amount;
        return new OrderSummary(perUserCents / 100m, perUserCents * userCount / 100m);
    }
}
