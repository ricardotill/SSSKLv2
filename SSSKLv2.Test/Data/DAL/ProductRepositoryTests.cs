using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class ProductRepositoryTests : RepositoryTest
{
    private MockDbContextFactory _dbContextFactory = null!;
    private ProductRepository _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = new MockDbContextFactory(GetOptions());
        _sut = new ProductRepository(_dbContextFactory);
        // Ensure a clean slate for each test (RepositoryTest seeds one product by default)
        ClearAllProducts().GetAwaiter().GetResult();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }

    [TestMethod]
    public async Task GetAll_WhenProductsInDb_ReturnAll()
    {
        // Arrange
        var p1 = NewProduct("name1", 30.32m, 50);
        var p2 = NewProduct("name2", 18.99m, 0);
        await SaveProducts(p1, p2);
        
        // Act
        var result = await _sut.GetAll();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(p1);
        result.Should().ContainEquivalentOf(p2);
    }
    
    [TestMethod]
    public async Task GetAll_WhenDbEmpty_ReturnNoProducts()
    {
        // Act
        var result = await _sut.GetAll();
        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetAllAvailable_WhenAllProductsInDbInStock_ReturnAllInStock()
    {
        // Arrange
        var p1 = NewProduct("name1", 30.32m, 50);
        var p2 = NewProduct("name2", 18.99m, 30);
        await SaveProducts(p1, p2);
        
        // Act
        var result = await _sut.GetAllAvailable();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(p1);
        result.Should().ContainEquivalentOf(p2);
    }

    [TestMethod]
    public async Task GetAllAvailable_WhenNotAllProductsInDbInStock_ReturnOnlyInStock()
    {
        // Arrange
        var p1 = NewProduct("name1", 30.32m, 1);
        var p2 = NewProduct("name2", 18.99m, 0);
        await SaveProducts(p1, p2);

        // Act
        var result = await _sut.GetAllAvailable();

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainEquivalentOf(p1);
        result.Should().NotContainEquivalentOf(p2);
    }

    [TestMethod]
    public async Task GetAllAvailable_WhenDbEmpty_ReturnNoProducts()
    {
        // Act
        var result = await _sut.GetAllAvailable();
        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetById_WhenInDb_ThenReturnProduct()
    {
        // Arrange
        var p1 = NewProduct("name1", 30.32m, 1);
        await SaveProducts(p1);
        
        // Act
        var result = await _sut.GetById(p1.Id);

        // Assert
        result.Should().BeEquivalentTo(p1);
    }

    [TestMethod]
    public async Task GetById_WhenNotInDb_ReturnNotFoundException()
    {
        // Act
        Func<Task<Product>> function = () => _sut.GetById(Guid.NewGuid());
        // Assert
        await function.Should().ThrowAsync<NotFoundException>();
    }

    [TestMethod]
    public async Task Create_WhenNewProduct_AddsProduct()
    {
        var p1 = NewProduct("name1", 30.32m, 1);
        await _sut.Create(p1);
        var dblist = await GetProducts();
        dblist.Should().ContainSingle().And.ContainEquivalentOf(p1);
    }

    [TestMethod]
    public async Task Create_WhenExistingId_ThrowsDbUpdateException()
    {
        var id = Guid.NewGuid();
        var p1 = NewProduct("name1", 30.32m, 1, id);
        var p2 = NewProduct("name2", 18.99m, 0, id);
        await SaveProducts(p1);
        Func<Task> act = () => _sut.Create(p2);
        await act.Should().ThrowAsync<DbUpdateException>();
        (await GetProducts()).Should().HaveCount(1).And.ContainEquivalentOf(p1);
    }
    
    [TestMethod]
    public async Task Create_WhenDuplicateName_RejectsSecondProductSinceUniqueConstraint()
    {
        var name = "dup";
        var p1 = NewProduct(name, 30.32m, 1);
        var p2 = NewProduct(name, 18.99m, 0);
        await SaveProducts(p1);
        var act = () => _ = _sut.Create(p2) ; // should fail (no unique constraint on Name)
        await act.Should().ThrowAsync<DbUpdateException>();
        var dblist = await GetProducts();
        dblist.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task Update_WhenExistingProduct_UpdatesFields()
    {
        var p1 = NewProduct("name1", 30.32m, 1);
        await SaveProducts(p1);
        var loaded = (await GetProducts()).Single(p => p.Id == p1.Id);
        loaded.Description = "changed";
        loaded.Price = 99.99m;
        await _sut.Update(loaded);
        var refreshed = (await GetProducts()).Single();
        refreshed.Description.Should().Be("changed");
        refreshed.Price.Should().Be(99.99m);
    }

    [TestMethod]
    public async Task Update_WhenProductNotInDb_ThrowsConcurrency()
    {
        var p1 = NewProduct("name1", 30.32m, 1);
        Func<Task> act = () => _sut.Update(p1);
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        (await GetProducts()).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Delete_WhenExistingProduct_RemovesIt()
    {
        var p1 = NewProduct("name1", 30.32m, 1);
        var p2 = NewProduct("name2", 18.99m, 0);
        await SaveProducts(p1, p2);
        await _sut.Delete(p1.Id);
        var dblist = await GetProducts();
        dblist.Should().HaveCount(1).And.ContainEquivalentOf(p2).And.NotContainEquivalentOf(p1);
    }
    
    [TestMethod]
    public async Task Delete_WhenProductNotFound_ThrowsNotFound()
    {
        var p1 = NewProduct("name1", 30.32m, 1);
        await SaveProducts(p1);
        Func<Task> act = () => _sut.Delete(Guid.NewGuid());
        await act.Should().ThrowAsync<NotFoundException>();
        (await GetProducts()).Should().HaveCount(1);
    }

    // Helpers
    private Product NewProduct(string name, decimal price, int stock, Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        Description = $"description-{name}",
        Price = price,
        Stock = stock,
        CreatedOn = DateTime.Now
    };

    private async Task ClearAllProducts()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Product.RemoveRange(context.Product);
        await context.SaveChangesAsync();
    }

    private async Task<IList<Product>> GetProducts()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Product.AsNoTracking().ToListAsync();
    }
    
    private async Task SaveProducts(params Product[] products)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Product.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
