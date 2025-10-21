using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Test.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class ApplicationUserRepositoryTests : RepositoryTest
{
    private MockDbContextFactory _dbContextFactory = null!;
    private ApplicationUserRepository _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = new MockDbContextFactory(GetOptions());
        _sut = new ApplicationUserRepository(_dbContextFactory);
        
        // Clean up any existing users except the TestUser
        CleanupUsersExceptTest().GetAwaiter().GetResult();
    }
    
    #region GetById Tests

    [TestMethod]
    public async Task GetById_WithExistingId_ReturnsUser()
    {
        // Arrange - TestUser is created in base class
        var userId = TestUser.Id;
        
        // Act
        var result = await _sut.GetById(userId);
        
        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.UserName.Should().Be("testuser");
    }
    
    [TestMethod]
    public async Task GetById_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid().ToString();
        
        // Act
        Func<Task> action = async () => await _sut.GetById(nonExistentId);
        
        // Assert
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("ApplicationUser not found");
    }
    
    [TestMethod]
    public async Task GetById_WithNullOrEmptyId_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetById(null!));
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetById(string.Empty));
    }
    
    #endregion
    
    #region GetByUsername Tests
    
    [TestMethod]
    public async Task GetByUsername_WithExistingUsername_ReturnsUser()
    {
        // Arrange - TestUser is created in base class
        
        // Act
        var result = await _sut.GetByUsername("testuser");
        
        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("testuser");
        result.Email.Should().Be(TestUser.Email);
    }
    
    [TestMethod]
    public async Task GetByUsername_WithNonExistentUsername_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentUsername = "nonexistentuser";
        
        // Act
        Func<Task> action = async () => await _sut.GetByUsername(nonExistentUsername);
        
        // Assert
        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("ApplicationUser not found");
    }
    
    [TestMethod]
    public async Task GetByUsername_WithNullOrEmptyUsername_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByUsername(null!));
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByUsername(string.Empty));
    }
    
    #endregion
    
    #region GetAllForAdmin Tests
    
    [TestMethod]
    public async Task GetAllForAdmin_WithMultipleUsers_ReturnsAllUsersOrderedByName()
    {
        // Arrange
        var additionalUsers = new[]
        {
            CreateUser("zuser", "z@test.com", "Zebra", "User"),
            CreateUser("auser", "a@test.com", "Alpha", "User")
        };
        
        await SaveUsers(additionalUsers);
        
        // Act
        var result = await _sut.GetAllForAdmin();
        
        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(3); // TestUser + 2 additional
        
        // Verify ordering by Name using StringComparer.CurrentCulture instead of CompareTo
        var orderedByName = result.OrderBy(u => u.Name, StringComparer.CurrentCulture).ToList();
        result.Should().ContainInOrder(orderedByName);
        
        // Verify all created users are present
        result.Should().Contain(u => u.UserName == "testuser");
        result.Should().Contain(u => u.UserName == "auser");
        result.Should().Contain(u => u.UserName == "zuser");
    }
    
    [TestMethod]
    public async Task GetAllForAdmin_WithNoUsers_ReturnsEmptyList()
    {
        // Arrange
        await DeleteAllUsers();
        
        // Act
        var result = await _sut.GetAllForAdmin();
        
        // Assert
        result.Should().BeEmpty();
    }
    
    #endregion
    
    #region GetAll Tests
    
    [TestMethod]
    public async Task GetAll_WithMultipleUsers_ReturnsConsumerUsersOrderedByLastOrdered()
    {
        // Arrange
        await SetupRolesAndUserRoles();
        
        var now = DateTime.Now;
        
        // Create users with different LastOrdered dates
        var user1 = CreateUser("user1", "user1@test.com", "User", "One");
        user1.LastOrdered = now.AddDays(-1);
        
        var user2 = CreateUser("user2", "user2@test.com", "User", "Two");
        user2.LastOrdered = now;
        
        await SaveUsers(user1, user2);
        
        // Set roles for all users
        await AddUserToRole(user1, "Consumer");
        await AddUserToRole(user2, "Consumer");
        
        // Create separate kiosk and guest users in a different batch to avoid conflicts
        var kioskUser = CreateUser("kioskuser", "kiosk@test.com", "Kiosk", "User");
        var guestUser = CreateUser("guestuser", "guest@test.com", "Guest", "User");
        
        await SaveUsers(kioskUser, guestUser);
        await AddUserToRole(kioskUser, "Kiosk");
        await AddUserToRole(guestUser, "Guest");
        
        // Act
        var result = await _sut.GetAll();
        
        // Assert
        // Should contain consumer users ordered by LastOrdered (descending)
        result.Should().Contain(u => u.UserName == "user1");
        result.Should().Contain(u => u.UserName == "user2");
        
        // Should not contain kiosk or guest users
        result.Should().NotContain(u => u.UserName == "kioskuser");
        result.Should().NotContain(u => u.UserName == "guestuser");
        
        // Verify ordering - most recent first
        var user1Index = -1;
        var user2Index = -1;
        
        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].UserName == "user1") user1Index = i;
            if (result[i].UserName == "user2") user2Index = i;
        }
        
        // Only verify if both users were found
        if (user1Index >= 0 && user2Index >= 0)
        {
            // user2 has more recent LastOrdered, so should come first
            user2Index.Should().BeLessThan(user1Index);
        }
    }
    
    [TestMethod]
    public async Task GetAll_WithNoConsumerUsers_ReturnsEmptyList()
    {
        // Arrange - clear database and set up roles
        await DeleteAllUsersAndRoles();
        await SetupRolesAndUserRoles();
        
        var kioskUser = CreateUser("kioskuser", "kiosk@test.com", "Kiosk", "User");
        var guestUser = CreateUser("guestuser", "guest@test.com", "Guest", "User");
        
        await SaveUsers(kioskUser, guestUser);
        
        await AddUserToRole(kioskUser, "Kiosk");
        await AddUserToRole(guestUser, "Guest");
        
        // Act
        var result = await _sut.GetAll();
        
        // Assert
        result.Should().BeEmpty();
    }
    
    #endregion
    
    #region GetAllWithOrders Tests
    
    [TestMethod]
    public async Task GetAllWithOrders_WithUsersAndOrders_ReturnsConsumerUsersWithOrdersOrderedByLastOrdered()
    {
        // Arrange
        await SetupRolesAndUserRoles();
        
        var now = DateTime.Now;
        
        // Create users with different LastOrdered dates
        var user1 = CreateUser("userWithOrders1", "user1@test.com", "User", "One");
        user1.LastOrdered = now.AddDays(-1);
        
        var user2 = CreateUser("userWithOrders2", "user2@test.com", "User", "Two");
        user2.LastOrdered = now;
        
        var userNoOrders = CreateUser("userNoOrders", "user3@test.com", "User", "Three");
        
        await SaveUsers(user1, user2, userNoOrders);
        
        // Set roles for users
        await AddUserToRole(user1, "Consumer");
        await AddUserToRole(user2, "Consumer");
        await AddUserToRole(userNoOrders, "Consumer");
        
        // Create product
        var product = await CreateProduct("Test Product", 10.0m);
        
        // Create orders for users
        await CreateOrder(user1, product);
        await CreateOrder(user2, product);
        
        // Act
        var result = await _sut.GetAllWithOrders();
        
        // Assert
        result.Should().Contain(u => u.UserName == "userWithOrders1");
        result.Should().Contain(u => u.UserName == "userWithOrders2");
        result.Should().NotContain(u => u.UserName == "userNoOrders"); // No orders
        
        // Verify ordering - most recent first
        var user1Index = -1;
        var user2Index = -1;
        
        for (int i = 0; i < result.Count; i++)
        {
            if (result[i].UserName == "userWithOrders1") user1Index = i;
            if (result[i].UserName == "userWithOrders2") user2Index = i;
        }
        
        // Only verify if both users were found
        if (user1Index >= 0 && user2Index >= 0)
        {
            // user2 has more recent LastOrdered, so should come first
            user2Index.Should().BeLessThan(user1Index);
        }
        
        // Verify orders are included
        foreach (var user in result)
        {
            user.Orders.Should().NotBeEmpty();
        }
    }
    
    [TestMethod]
    public async Task GetAllWithOrders_WithNoUsersWithOrders_ReturnsEmptyList()
    {
        // Arrange
        await DeleteAllUsersAndRoles();
        await SetupRolesAndUserRoles();
        
        var userNoOrders = CreateUser("userNoOrders", "user@test.com", "User", "NoOrders");
        await SaveUsers(userNoOrders);
        await AddUserToRole(userNoOrders, "Consumer");
        
        // Act
        var result = await _sut.GetAllWithOrders();
        
        // Assert
        result.Should().BeEmpty();
    }
    
    #endregion
    
    #region GetFirst12WithOrders Tests
    
    [TestMethod]
    public async Task GetFirst12WithOrders_WithMoreThan12Users_ReturnsFirst10Users()
    {
        // Arrange
        await SetupRolesAndUserRoles();
        
        // Create 15 users with orders
        var users = new List<ApplicationUser>();
        var product = await CreateProduct("Test Product", 10.0m);
        
        for (int i = 0; i < 15; i++)
        {
            string username = $"user{i}";
            var user = CreateUser(username, $"{username}@test.com", "User", $"{i}");
            user.LastOrdered = DateTime.Now.AddHours(-i); // Different LastOrdered dates
            users.Add(user);
        }
        
        await SaveUsers(users.ToArray());
        
        // Assign roles and create orders
        foreach (var user in users)
        {
            await AddUserToRole(user, "Consumer");
            await CreateOrder(user, product);
        }
        
        // Act
        var result = await _sut.GetFirst12WithOrders();
        
        // Assert
        result.Should().HaveCount(10); // Method actually takes 10, not 12 as the name suggests
        
        // Verify most recently ordered users are included
        var expectedUsers = users.Take(10).ToList();
        foreach (var expectedUser in expectedUsers)
        {
            result.Should().Contain(u => u.UserName == expectedUser.UserName);
        }
        
        // Verify older orders are excluded
        var excludedUsers = users.Skip(10).ToList();
        foreach (var excludedUser in excludedUsers)
        {
            result.Should().NotContain(u => u.UserName == excludedUser.UserName);
        }
    }
    
    [TestMethod]
    public async Task GetFirst12WithOrders_WithFewerThan12Users_ReturnsAllUsers()
    {
        // Arrange
        await SetupRolesAndUserRoles();
        
        // Create 5 users with orders
        var users = new List<ApplicationUser>();
        var product = await CreateProduct("Test Product", 10.0m);
        
        for (int i = 0; i < 5; i++)
        {
            string username = $"user{i}";
            var user = CreateUser(username, $"{username}@test.com", "User", $"{i}");
            user.LastOrdered = DateTime.Now.AddHours(-i);
            users.Add(user);
        }
        
        await SaveUsers(users.ToArray());
        
        // Assign roles and create orders
        foreach (var user in users)
        {
            await AddUserToRole(user, "Consumer");
            await CreateOrder(user, product);
        }
        
        // Act
        var result = await _sut.GetFirst12WithOrders();
        
        // Assert
        result.Should().HaveCount(5);
        
        // Verify all users are included
        foreach (var expectedUser in users)
        {
            result.Should().Contain(u => u.UserName == expectedUser.UserName);
        }
    }
    
    [TestMethod]
    public async Task GetFirst12WithOrders_WithNoUsersWithOrders_ReturnsEmptyList()
    {
        // Arrange
        await DeleteAllUsersAndRoles();
        await SetupRolesAndUserRoles();
        
        // Act
        var result = await _sut.GetFirst12WithOrders();
        
        // Assert
        result.Should().BeEmpty();
    }
    
    #endregion
    
    #region Exception Handling Tests
    
    [TestMethod]
    public async Task Repository_WhenDbContextFactoryThrowsException_PropagatesException()
    {
        // Arrange
        var mockFactory = Substitute.For<IDbContextFactory<ApplicationDbContext>>();
        mockFactory.CreateDbContextAsync().Returns(Task.FromException<ApplicationDbContext>(new InvalidOperationException("Database error")));
        
        var repository = new ApplicationUserRepository(mockFactory);
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetById("testid"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetByUsername("testuser"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetAll());
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetAllForAdmin());
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetAllWithOrders());
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetFirst12WithOrders());
    }
    
    #endregion

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }
    
    #region Helper Methods
    
    private ApplicationUser CreateUser(string username, string email, string name, string surname)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            NormalizedUserName = username.ToUpper(),
            Email = email,
            NormalizedEmail = email.ToUpper(),
            Name = name,
            Surname = surname,
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }
    
    private async Task SaveUsers(params ApplicationUser[] users)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        foreach (var user in users)
        {
            // Check if user already exists to prevent unique constraint violations
            var existingUser = await context.Users.FindAsync(user.Id);
            if (existingUser == null)
            {
                await context.Users.AddAsync(user);
            }
        }
        await context.SaveChangesAsync();
    }
    
    private async Task DeleteAllUsers()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var users = await context.Users.ToListAsync();
        context.Users.RemoveRange(users);
        await context.SaveChangesAsync();
    }
    
    private async Task DeleteAllUsersAndRoles()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        context.UserRoles.RemoveRange(context.UserRoles);
        await context.SaveChangesAsync();
        
        context.Users.RemoveRange(context.Users);
        await context.SaveChangesAsync();
        
        context.Roles.RemoveRange(context.Roles);
        await context.SaveChangesAsync();
    }
    
    private async Task CleanupUsersExceptTest()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var usersToDelete = await context.Users
            .Where(u => u.UserName != "testuser")
            .ToListAsync();
        
        context.Users.RemoveRange(usersToDelete);
        await context.SaveChangesAsync();
    }
    
    private async Task SetupRolesAndUserRoles()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Clear existing roles and user roles
        context.UserRoles.RemoveRange(context.UserRoles);
        await context.SaveChangesAsync();
        
        context.Roles.RemoveRange(context.Roles);
        await context.SaveChangesAsync();
        
        // Create roles
        var roles = new IdentityRole[]
        {
            new() { Id = Guid.NewGuid().ToString(), Name = "Admin", NormalizedName = "ADMIN" },
            new() { Id = Guid.NewGuid().ToString(), Name = "Consumer", NormalizedName = "CONSUMER" },
            new() { Id = Guid.NewGuid().ToString(), Name = "Kiosk", NormalizedName = "KIOSK" },
            new() { Id = Guid.NewGuid().ToString(), Name = "Guest", NormalizedName = "GUEST" }
        };
        
        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }
    
    private async Task AddUserToRole(ApplicationUser user, string roleName)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName);
        
        if (role == null)
        {
            throw new InvalidOperationException($"Role {roleName} not found");
        }
        
        // Check if user already has this role
        var existingUserRole = await context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
            
        if (existingUserRole == null)
        {
            await context.UserRoles.AddAsync(new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            });
            
            await context.SaveChangesAsync();
        }
    }
    
    private async Task<Product> CreateProduct(string name, decimal price)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Check if product with this name already exists
        var existingProduct = await context.Product
            .FirstOrDefaultAsync(p => p.Name == name);
            
        if (existingProduct != null)
        {
            return existingProduct;
        }
        
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = $"Description for {name}",
            Price = price,
            Stock = 10,
            CreatedOn = DateTime.Now
        };
        
        await context.Product.AddAsync(product);
        await context.SaveChangesAsync();
        
        return product;
    }
    
    private async Task CreateOrder(ApplicationUser user, Product product)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        
        // Get tracked instances of user and product
        var trackedUser = await context.Users.FindAsync(user.Id);
        var trackedProduct = await context.Product.FindAsync(product.Id);
        
        if (trackedUser == null || trackedProduct == null)
        {
            throw new InvalidOperationException("User or product not found");
        }
        
        var order = new Order
        {
            Id = Guid.NewGuid(),
            User = trackedUser,
            Product = trackedProduct,
            Amount = 1,
            Paid = trackedProduct.Price,
            ProductNaam = trackedProduct.Name,
            CreatedOn = DateTime.Now
        };
        
        await context.Order.AddAsync(order);
        await context.SaveChangesAsync();
    }
    
    #endregion
}

