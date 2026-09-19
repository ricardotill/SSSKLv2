using FluentAssertions;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class UserStatRepositoryTests : RepositoryTest
{
    private UserStatRepository _sut = null!;
    private IProductUserStatRepository _productStats = null!;

    [TestInitialize]
    public void Initialize()
    {
        InitializeDatabase();
        _productStats = Substitute.For<IProductUserStatRepository>();
        _sut = new UserStatRepository(new ApplicationDbContext(GetOptions()), _productStats);
    }

    [TestCleanup]
    public void Cleanup() => CleanupDatabase();

    [TestMethod]
    public async Task GetOrCreateByUserId_CreatesAndReusesStat()
    {
        var first = await _sut.GetOrCreateByUserId(TestUser.Id);
        var second = await _sut.GetOrCreateByUserId(TestUser.Id);

        second.Id.Should().Be(first.Id);
        (await _sut.GetByUserId(TestUser.Id)).Should().BeEquivalentTo(first);
    }

    [TestMethod]
    public async Task RecalculateByUserId_RebuildsAggregatesAndDelegatesProductStats()
    {
        var user = TestUser;
        var firstOrderDate = DateTime.UtcNow.AddDays(-2);
        var secondOrderDate = DateTime.UtcNow.AddDays(-1);
        var topUpDate = DateTime.UtcNow.AddHours(-12);
        await using (var context = new ApplicationDbContext(GetOptions()))
        {
            user = context.Users.Single(candidate => candidate.Id == user.Id);
            context.UserStats.Add(new UserStat { UserId = user.Id, TotalSpent = 999 });
            context.Order.AddRange(
                new Order { User = user, ProductNaam = "one", Amount = 2, Paid = 10, CreatedOn = firstOrderDate },
                new Order { User = user, ProductNaam = "two", Amount = 3, Paid = 20, CreatedOn = secondOrderDate });
            context.TopUp.Add(new TopUp { User = user, Saldo = 25, CreatedOn = topUpDate });
            context.SaveChanges();
        }

        var result = await _sut.RecalculateByUserId(user.Id);

        result.TotalSpent.Should().Be(30);
        result.TotalOrders.Should().Be(2);
        result.TotalItemsBought.Should().Be(5);
        result.TotalTopUp.Should().Be(25);
        result.MaxSingleTopUp.Should().Be(25);
        result.MembershipStartDate.Should().Be(firstOrderDate);
        result.LastOrderDate.Should().Be(secondOrderDate);
        result.LastTopUpDate.Should().Be(topUpDate);
        result.CurrentStreak.Should().Be(3);
        await _productStats.Received(1).RecalculateByUserId(user.Id);
    }

    [TestMethod]
    public async Task RecalculateByUserId_WithNoActivityResetsCounters()
    {
        var result = await _sut.RecalculateByUserId(TestUser.Id);

        result.TotalOrders.Should().Be(0);
        result.TotalSpent.Should().Be(0);
        result.CurrentStreak.Should().Be(0);
        result.MaxOrdersPerHour.Should().Be(0);
        result.MinMinutesBetweenOrders.Should().Be(int.MaxValue);
    }

    [TestMethod]
    public async Task Update_PersistsChanges()
    {
        var stat = await _sut.GetOrCreateByUserId(TestUser.Id);
        stat.TotalOrders = 7;

        await _sut.Update(stat);

        (await _sut.GetByUserId(TestUser.Id)).TotalOrders.Should().Be(7);
    }
}