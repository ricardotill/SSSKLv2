using FluentAssertions;
using SSSKL.Mobile.Core.ViewModels;

namespace SSSKL.Mobile.Test.ViewModels;

[TestClass]
public class OrderPricingTests
{
    [TestMethod]
    public void Calculate_NothingSelected_ReturnsNull()
    {
        OrderPricing.Calculate([], 1, 1, false).Should().BeNull();
        OrderPricing.Calculate([1m], 0, 1, false).Should().BeNull();
    }

    [TestMethod]
    public void Calculate_NoSplit_EveryoneBuysTheFullAmount() =>
        OrderPricing.Calculate([1.25m, 0.50m], 2, 3, false).Should().Be(new OrderSummary(5.25m, 10.50m));

    [TestMethod]
    public void Calculate_Split_RoundsEachProductUpPerPerson() =>
        // 1.00 / 3 = 0.333.. -> 0.34 per person, times 2
        OrderPricing.Calculate([1.00m], 3, 2, true).Should().Be(new OrderSummary(0.68m, 2.04m));

    [TestMethod]
    public void Calculate_SplitWithOneUser_IsIgnored() =>
        OrderPricing.Calculate([1.00m], 1, 1, true).Should().Be(new OrderSummary(1.00m, 1.00m));
}
