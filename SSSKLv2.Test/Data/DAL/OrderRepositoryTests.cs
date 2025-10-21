using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class OrderRepositoryTests : RepositoryTest
{
    private MockDbContextFactory _dbContextFactory = null!;
    private OrderRepository _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = new MockDbContextFactory(GetOptions());
        _sut = new OrderRepository(_dbContextFactory);
    }
    
    #region GetAllAsync Tests
    
    [TestMethod]
    public async Task GetAllAsync_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsOrdersOrderedByCreatedOnDescending()
    {
        // Arrange
        var order1 = CreateTestOrder(DateTime.Now.AddHours(-2));
        var order2 = CreateTestOrder(DateTime.Now.AddHours(-1));
        var order3 = CreateTestOrder(DateTime.Now);
        
        await SaveOrdersDirectly(order1, order2, order3);
        
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result[0].CreatedOn >= result[1].CreatedOn);
        Assert.IsTrue(result[1].CreatedOn >= result[2].CreatedOn);
        Assert.IsNotNull(result[0].User);
    }
    
    #endregion

    #region GetAllQueryable Tests

    [TestMethod]
    public async Task GetAllQueryable_WithEmptyDatabase_ReturnsEmptyQueryable()
    {
        // Arrange
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Act
        var result = _sut.GetAllQueryable(context);
        var materializedResult = await result.ToListAsync();

        // Assert
        Assert.AreEqual(0, materializedResult.Count);
    }

    [TestMethod]
    public async Task GetAllQueryable_ReturnsQueryableWithUserIncluded()
    {
        // Arrange
        var order1 = CreateTestOrder(DateTime.Now.AddHours(-1));
        var order2 = CreateTestOrder(DateTime.Now);
        
        await SaveOrdersDirectly(order1, order2);
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Act
        var result = _sut.GetAllQueryable(context);
        var materializedResult = await result.ToListAsync();

        // Assert
        Assert.AreEqual(2, materializedResult.Count);
        Assert.IsNotNull(materializedResult[0].User);
        Assert.IsTrue(materializedResult[0].CreatedOn >= materializedResult[1].CreatedOn);
    }

    #endregion

    #region GetPersonalQueryable Tests

    [TestMethod]
    public async Task GetPersonalQueryable_WithNonExistentUser_ReturnsEmptyQueryable()
    {
        // Arrange
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Act
        var result = _sut.GetPersonalQueryable("nonexistent", context);
        var materializedResult = await result.ToListAsync();

        // Assert
        Assert.AreEqual(0, materializedResult.Count);
    }

    [TestMethod]
    public async Task GetPersonalQueryable_ReturnsOrdersForSpecificUser()
    {
        // Arrange
        var user2 = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "testuser2", Email = "test2@test.com", Name = "Test2", Surname = "User2" };
        await SaveUserDirectly(user2);

        var order1 = CreateTestOrder(DateTime.Now.AddHours(-1), TestUser);
        var order2 = CreateTestOrder(DateTime.Now, user2);
        
        await SaveOrdersDirectly(order1, order2);
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Act
        var result = _sut.GetPersonalQueryable("testuser", context);
        var materializedResult = await result.ToListAsync();

        // Assert
        Assert.AreEqual(1, materializedResult.Count);
        Assert.AreEqual("testuser", materializedResult[0].User.UserName);
    }

    #endregion

    #region GetLatest Tests

    [TestMethod]
    public async Task GetLatest_WithNoRecentOrders_ReturnsEmptyList()
    {
        // Arrange
        var order = CreateTestOrder(DateTime.Now.AddHours(-15));
        await SaveOrdersDirectly(order);

        // Act
        var result = await _sut.GetLatest();

        // Assert
        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public async Task GetLatest_ReturnsOrdersFromLast12Hours()
    {
        // Arrange
        var now = DateTime.Now;
        var order1 = CreateTestOrder(now.AddHours(-13)); // Too old
        var order2 = CreateTestOrder(now.AddHours(-6));  // Within 12 hours
        var order3 = CreateTestOrder(now.AddHours(-1));  // Within 12 hours
        
        await SaveOrdersDirectly(order1, order2, order3);

        // Act
        var result = await _sut.GetLatest();
        var resultList = result.ToList();

        // Assert
        Assert.AreEqual(2, resultList.Count);
        Assert.IsTrue(resultList.All(o => o.CreatedOn > now.AddHours(-12)));
        Assert.IsNotNull(resultList.First().User);
    }

    [TestMethod]
    public async Task GetLatest_WithMoreThan10RecentOrders_ReturnsTop10()
    {
        // Arrange
        var orders = new List<Order>();
        for (int i = 0; i < 15; i++)
        {
            orders.Add(CreateTestOrder(DateTime.Now.AddMinutes(-i)));
        }
        await SaveOrdersDirectly(orders.ToArray());

        // Act
        var result = await _sut.GetLatest();

        // Assert
        Assert.AreEqual(10, result.Count());
    }

    #endregion

    #region GetById Tests

    [TestMethod]
    public async Task GetById_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetById(invalidId));
    }

    [TestMethod]
    public async Task GetById_WithValidId_ReturnsOrder()
    {
        // Arrange
        var order = CreateTestOrder(DateTime.Now);
        await SaveOrdersDirectly(order);

        // Act
        var result = await _sut.GetById(order.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(order.Id, result.Id);
        Assert.IsNotNull(result.User);
    }

    #endregion

    #region CreateRange Tests

    [TestMethod]
    public async Task CreateRange_WithEmptyList_DoesNothing()
    {
        // Arrange
        var orders = new List<Order>();

        // Act
        await _sut.CreateRange(orders);

        // Assert
        var savedOrders = await GetOrdersDirectly();
        Assert.AreEqual(0, savedOrders.Count);
    }

    [TestMethod]
    public async Task CreateRange_CreatesOrdersAndUpdatesUserSaldo()
    {
        // Arrange
        TestUser.Saldo = 100m;
        await UpdateUserDirectly(TestUser);

        var order = new Order 
        { 
            Id = Guid.NewGuid(), 
            CreatedOn = DateTime.Now, 
            User = TestUser, 
            Product = null,
            ProductNaam = "TestProduct",
            Amount = 5,
            Paid = 25m
        };

        // Act
        await _sut.CreateRange([order]);

        // Assert
        var savedOrders = await GetOrdersDirectly();
        Assert.AreEqual(1, savedOrders.Count);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var updatedUser = await context.Users.FindAsync(TestUser.Id);
        Assert.AreEqual(75m, updatedUser!.Saldo); // 100 - 25
    }

    [TestMethod]
    public async Task CreateRange_CreatesOrdersAndUpdatesProductStock()
    {
        // Arrange
        TestUser.Saldo = 100m;
        TestProduct.Stock = 50;
        await UpdateUserDirectly(TestUser);
        await UpdateProductDirectly(TestProduct);

        var order = new Order 
        { 
            Id = Guid.NewGuid(), 
            CreatedOn = DateTime.Now, 
            User = TestUser, 
            Product = TestProduct,
            ProductNaam = TestProduct.Name,
            Amount = 5,
            Paid = 25m
        };

        // Act
        await _sut.CreateRange([order]);

        // Assert
        var savedOrders = await GetOrdersDirectly();
        Assert.AreEqual(1, savedOrders.Count);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var updatedUser = await context.Users.FindAsync(TestUser.Id);
        var updatedProduct = await context.Product.FindAsync(TestProduct.Id);
        
        Assert.AreEqual(75m, updatedUser!.Saldo); // 100 - 25
        Assert.AreEqual(45, updatedProduct!.Stock); // 50 - 5
    }

    #endregion

    #region Delete Tests

    [TestMethod]
    public async Task Delete_WithInvalidId_ThrowsNotFoundException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _sut.Delete(invalidId));
        Assert.AreEqual("Order Not Found", exception.Message);
    }

    [TestMethod]
    public async Task Delete_WithValidId_DeletesOrderAndRevertsUserSaldo()
    {
        // Arrange
        TestUser.Saldo = 75m;
        await UpdateUserDirectly(TestUser);

        var order = CreateTestOrder(DateTime.Now, TestUser, paid: 25m);
        await SaveOrdersDirectly(order);

        // Act
        await _sut.Delete(order.Id);

        // Assert
        var remainingOrders = await GetOrdersDirectly();
        Assert.AreEqual(0, remainingOrders.Count);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var updatedUser = await context.Users.FindAsync(TestUser.Id);
        Assert.AreEqual(100m, updatedUser!.Saldo); // 75 + 25
    }

    [TestMethod]
    public async Task Delete_WithValidId_DeletesOrderAndRevertsProductStock()
    {
        // Arrange
        TestUser.Saldo = 75m;
        TestProduct.Stock = 45;
        await UpdateUserDirectly(TestUser);
        await UpdateProductDirectly(TestProduct);

        var order = CreateTestOrder(DateTime.Now, TestUser, paid: 25m, 5);
        order.ProductNaam = TestProduct.Name; // Ensure product name matches
        await SaveOrdersDirectly(order);

        // Act
        await _sut.Delete(order.Id);

        // Assert
        var remainingOrders = await GetOrdersDirectly();
        Assert.AreEqual(0, remainingOrders.Count);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var updatedUser = await context.Users.FindAsync(TestUser.Id);
        var updatedProduct = await context.Product.FindAsync(TestProduct.Id);
        
        Assert.AreEqual(100m, updatedUser!.Saldo); // 75 + 25
        Assert.AreEqual(50, updatedProduct!.Stock); // 45 + 5
    }

    [TestMethod]
    public async Task Delete_WithNonMatchingProductName_OnlyRevertsUserSaldo()
    {
        // Arrange
        TestUser.Saldo = 75m;
        TestProduct.Stock = 45;
        await UpdateUserDirectly(TestUser);
        await UpdateProductDirectly(TestProduct);

        var order = CreateTestOrder(DateTime.Now, TestUser, paid: 25m);
        order.ProductNaam = "DifferentProduct"; // Different from TestProduct.Name
        await SaveOrdersDirectly(order);

        // Act
        await _sut.Delete(order.Id);

        // Assert
        var remainingOrders = await GetOrdersDirectly();
        Assert.AreEqual(0, remainingOrders.Count);

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var updatedUser = await context.Users.FindAsync(TestUser.Id);
        var updatedProduct = await context.Product.FindAsync(TestProduct.Id);
        
        Assert.AreEqual(100m, updatedUser!.Saldo); // 75 + 25 (reverted)
        Assert.AreEqual(45, updatedProduct!.Stock); // 45 (not reverted because name doesn't match)
    }

    #endregion

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }
    
    #region Helper Methods
    
    private Order CreateTestOrder(DateTime createdOn, ApplicationUser? user = null, decimal paid = 10.00m, int amount = 1)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CreatedOn = createdOn,
            User = user ?? TestUser,
            Product = TestProduct,
            Amount = amount,
            Paid = paid,
            ProductNaam = "Test"
        };
    }
    
    private async Task<IList<Order>> GetOrdersDirectly()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Order.AsNoTracking().ToListAsync();
    }
    
    private async Task SaveOrdersDirectly(params Order[] orders)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        foreach (var order in orders)
        {
            // Ensure we use the existing tracked entities
            var existingUser = await context.Users.FindAsync(order.User.Id);
            if (existingUser != null)
            {
                order.User = existingUser;
            }
            
            if (order.Product != null)
            {
                var existingProduct = await context.Product.FindAsync(order.Product.Id);
                if (existingProduct != null)
                {
                    order.Product = existingProduct;
                }
            }
        }
        
        context.Order.AddRange(orders);
        await context.SaveChangesAsync();
    }

    private async Task SaveUserDirectly(ApplicationUser user)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    private async Task UpdateUserDirectly(ApplicationUser user)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    private async Task UpdateProductDirectly(Product product)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Product.Update(product);
        await context.SaveChangesAsync();
    }
    
    #endregion

    #region GetOrdersFromPastTwoYearsAsync Tests

    [TestMethod]
    public async Task GetOrdersFromPastTwoYearsAsync_WithNoOrders_ReturnsEmptyList()
    {
        var result = await _sut.GetOrdersFromPastTwoYearsAsync();
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetOrdersFromPastTwoYearsAsync_WithAllOrdersOlderThanTwoYears_ReturnsEmptyList()
    {
        var order1 = CreateTestOrder(DateTime.Now.AddYears(-3));
        var order2 = CreateTestOrder(DateTime.Now.AddYears(-5));
        await SaveOrdersDirectly(order1, order2);
        var result = await _sut.GetOrdersFromPastTwoYearsAsync();
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetOrdersFromPastTwoYearsAsync_WithAllOrdersWithinTwoYears_ReturnsAll()
    {
        var order1 = CreateTestOrder(DateTime.Now.AddMonths(-6));
        var order2 = CreateTestOrder(DateTime.Now.AddMonths(-18));
        await SaveOrdersDirectly(order1, order2);
        var result = await _sut.GetOrdersFromPastTwoYearsAsync();
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(o => o.CreatedOn >= DateTime.Now.AddYears(-2)));
    }

    [TestMethod]
    public async Task GetOrdersFromPastTwoYearsAsync_WithMixedOrders_ReturnsOnlyRecent()
    {
        var oldOrder = CreateTestOrder(DateTime.Now.AddYears(-3));
        var recentOrder = CreateTestOrder(DateTime.Now.AddMonths(-3));
        await SaveOrdersDirectly(oldOrder, recentOrder);
        var result = await _sut.GetOrdersFromPastTwoYearsAsync();
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(recentOrder.Id, result[0].Id);
    }
    #endregion
}
