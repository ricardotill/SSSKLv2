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
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 50,
            CreatedOn = DateTime.Now
        };
        var p2 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name2",
            Description = "description2",
            Price = 18.99m,
            Stock = 0,
            CreatedOn = DateTime.Now
        };
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
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 50,
            CreatedOn = DateTime.Now
        };
        var p2 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name2",
            Description = "description2",
            Price = 18.99m,
            Stock = 30,
            CreatedOn = DateTime.Now
        };
        await SaveProducts(p1, p2);
        
        // Act
        var result = await _sut.GetAllAvailable();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(p1);
        result.Should().ContainEquivalentOf(p2);
    }
    
    [TestMethod]
    public async Task GetAllAvailable_WhenNotAllProductsInDbInStock_ReturnAllInStock()
    {
        // Arrange
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 1,
            CreatedOn = DateTime.Now
        };
        var p2 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name2",
            Description = "description2",
            Price = 18.99m,
            Stock = 0,
            CreatedOn = DateTime.Now
        };
        await SaveProducts(p1, p2);
        
        // Act
        var result = await _sut.GetAllAvailable();

        // Assert
        result.Should().HaveCount(1);
        result.Should().ContainEquivalentOf(p1);
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
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 1,
            CreatedOn = DateTime.Now
        };
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
    public async Task Create_WhenNewProduct_ThenAddNewProduct()
    {
        // Arrange
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 1,
            CreatedOn = DateTime.Now
        };
        
        // Act
        await _sut.Create(p1);
        
        // Assert
        var dblist = await GetProducts();
        dblist.Should().HaveCount(1);
        dblist.Should().ContainEquivalentOf(p1);
    }

    [TestMethod]
    public async Task Create_WhenExistingId_ThenThrowDbException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var p1 = new Product()
        {
            Id = id,
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 1,
            CreatedOn = DateTime.Now
        };
        var p2 = new Product()
        {
            Id = id,
            Name = "name2",
            Description = "description2",
            Price = 18.99m,
            Stock = 0,
            CreatedOn = DateTime.Now
        };
        await SaveProducts(p1);
        
        // Act
        Func<Task> function = () => _sut.Create(p2);

        // Assert
        await function.Should().ThrowAsync<DbUpdateException>();
        var dblist = await GetProducts();
        dblist.Should().NotContainEquivalentOf(p2);
        dblist.Should().HaveCount(1);
    }
    
    [TestMethod]
    public async Task Create_WhenExistingUsername_ThenThrowDbException()
    {
        // Arrange
        var name = "name";
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "description1",
            Price = 30.32m,
            Stock = 1,
            CreatedOn = DateTime.Now
        };
        var p2 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "description2",
            Price = 18.99m,
            Stock = 0,
            CreatedOn = DateTime.Now
        };
        await SaveProducts(p1);
        
        // Act
        Func<Task> function = () => _sut.Create(p2);

        // Assert
        await function.Should().ThrowAsync<DbUpdateException>();
        var dblist = await GetProducts();
        dblist.Should().NotContainEquivalentOf(p2);
        dblist.Should().HaveCount(1);
    }
    
    [TestMethod]
    public async Task Update_WhenExistingAnnouncement_ThenUpdate()
    {
        // Arrange
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 1,
            CreatedOn = DateTime.Now
        };
        await SaveProducts(p1);
        var p1_update = (await GetProducts()).FirstOrDefault(e => e.Id == p1.Id);
        p1_update.Description = "desctest2";

        // Act
        await _sut.Update(p1_update);

        // Assert
        var dblist = await GetProducts();
        dblist.Should().ContainEquivalentOf(p1_update);
    }
    
    [TestMethod]
    public async Task Update_WhenNotExistingAnnouncement_ThenThrowDbException()
    {
        // Arrange
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 1,
            CreatedOn = DateTime.Now
        };

        // Act
        Func<Task> function = () => _sut.Update(p1);

        // Assert
        await function.Should().ThrowAsync<DbUpdateConcurrencyException>();
        var dblist = await GetProducts();
        dblist.Should().BeEmpty();
    }
    
    [TestMethod]
    public async Task Delete_WhenExistingOldUserMigration_ThenDeleteOldUserMigration()
    {
        // Arrange
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 1,
            CreatedOn = DateTime.Now
        };
        var p2 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name2",
            Description = "description2",
            Price = 18.99m,
            Stock = 0,
            CreatedOn = DateTime.Now
        };
        await SaveProducts(p1, p2);

        // Act
        await _sut.Delete(p1.Id);

        // Assert
        var dblist = await GetProducts();
        dblist.Should().NotContainEquivalentOf(p1);
        dblist.Should().HaveCount(1);
    }
    
    [TestMethod]
    public async Task Delete_WhenNotExistingProduct_ThenThrowDbException()
    {
        // Arrange
        var p1 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name1",
            Description = "description1",
            Price = 30.32m,
            Stock = 1,
            CreatedOn = DateTime.Now
        };
        var p2 = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "name2",
            Description = "description2",
            Price = 18.99m,
            Stock = 0,
            CreatedOn = DateTime.Now
        };
        await SaveProducts(p1);

        // Act
        Func<Task> function = () => _sut.Delete(p2.Id);

        // Assert
        await function.Should().ThrowAsync<NotFoundException>();
        var dblist = await GetProducts();
        dblist.Should().HaveCount(1);
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
