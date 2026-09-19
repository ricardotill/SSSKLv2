using FluentAssertions;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class ProductUserStatRepositoryTests : RepositoryTest
{
    private ProductUserStatRepository _sut = null!;

    [TestInitialize]
    public void Initialize()
    {
        InitializeDatabase();
        _sut = new ProductUserStatRepository(new ApplicationDbContext(GetOptions()));
    }

    [TestCleanup]
    public void Cleanup() => CleanupDatabase();

    [TestMethod]
    public async Task GetOrCreate_ReturnsExistingAndCreatesMissingRows()
    {
        var productId = TestProduct.Id;

        var created = await _sut.GetOrCreate(TestUser.Id, productId);
        var existing = await _sut.GetOrCreate(TestUser.Id, productId);

        existing.Id.Should().Be(created.Id);
        (await _sut.GetByUserId(TestUser.Id)).Should().ContainSingle();
        (await _sut.GetByUserAndProduct(TestUser.Id, Guid.NewGuid())).Should().BeNull();
    }

    [TestMethod]
    public async Task RecalculateByUserId_CalculatesProductTotalsAndRemovesStaleRows()
    {
        var productId = TestProduct.Id;
        var staleProductId = Guid.NewGuid();
        var first = DateTime.UtcNow.AddDays(-2);
        var last = DateTime.UtcNow.AddDays(-1);
        await using (var context = new ApplicationDbContext(GetOptions()))
        {
            context.Product.Add(new Product { Id = staleProductId, Name = "stale", Price = 1 });
            context.ProductUserStats.Add(new ProductUserStat { UserId = TestUser.Id, ProductId = staleProductId });
            var user = context.Users.Single(user => user.Id == TestUser.Id);
            var product = context.Product.Single(product => product.Id == productId);
            context.Order.AddRange(
                new Order { User = user, Product = product, ProductNaam = "product", Amount = 2, Paid = 10, CreatedOn = first },
                new Order { User = user, Product = product, ProductNaam = "product", Amount = 3, Paid = 20, CreatedOn = last });
            context.SaveChanges();
        }

        var result = await _sut.RecalculateByUserId(TestUser.Id);

        result.Should().ContainSingle();
        result[0].ProductId.Should().Be(productId);
        result[0].TotalAmount.Should().Be(5);
        result[0].TotalOrders.Should().Be(2);
        result[0].TotalSpent.Should().Be(30);
        result[0].LastOrderDate.Should().Be(last);
        (await _sut.GetByUserAndProduct(TestUser.Id, staleProductId)).Should().BeNull();
    }

    [TestMethod]
    public async Task Update_PersistsProductStats()
    {
        var stat = await _sut.GetOrCreate(TestUser.Id, TestProduct.Id);
        stat.TotalOrders = 4;

        await _sut.Update(stat);

        (await _sut.GetByUserId(TestUser.Id)).Single().TotalOrders.Should().Be(4);
    }
}