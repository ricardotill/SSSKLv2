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
        
        // Ensure roles are set up in the database
        await SetupRolesAndUserRoles();
        
        // Assign roles to ensure they are captured by the repository's join query
        foreach (var user in additionalUsers.Concat(new[] { TestUser }))
        {
            await AddUserToRole(user, "Consumer");
        }
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

    [TestMethod]
    public async Task GetAllForAdminPaged_ReturnsCorrectPage()
    {
        // Arrange
        var users = new[]
        {
            CreateUser("user1", "u1@test.com", "A", "User"),
            CreateUser("user2", "u2@test.com", "B", "User"),
            CreateUser("user3", "u3@test.com", "C", "User")
        };
        await SaveUsers(users);

        // Act
        var result = await _sut.GetAllForAdminPaged(1, 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserName.Should().Be("user2");
    }

    [TestMethod]
    public async Task GetCountAll_ReturnsCorrectCount()
    {
        // Arrange
        await DeleteAllUsers();
        var users = new[]
        {
            CreateUser("user1", "u1@test.com", "A", "User"),
            CreateUser("user2", "u2@test.com", "B", "User")
        };
        await SaveUsers(users);

        // Act
        var result = await _sut.GetCountAll();

        // Assert
        result.Should().Be(2);
    }
    
    #endregion
    
    #region GetAll Tests

    [TestMethod]
    public async Task GetCount_ReturnsOnlyConsumerUsers()
    {
        // Arrange
        await DeleteAllUsersAndRoles();
        await SetupRolesAndUserRoles();
        
        var consumer = CreateUser("consumer", "c@test.com", "C", "User");
        var kiosk = CreateUser("kiosk", "k@test.com", "K", "User");
        await SaveUsers(consumer, kiosk);
        
        await AddUserToRole(consumer, "Consumer");
        await AddUserToRole(kiosk, "Kiosk");

        // Act
        var result = await _sut.GetCount();

        // Assert
        result.Should().Be(1);
    }

    [TestMethod]
    public async Task GetAllPaged_ReturnsCorrectPageOfConsumerUsers()
    {
        // Arrange
        await DeleteAllUsersAndRoles();
        await SetupRolesAndUserRoles();
        
        var now = DateTime.Now;
        var u1 = CreateUser("u1", "u1@test.com", "U1", "User");
        u1.LastOrdered = now;
        var u2 = CreateUser("u2", "u2@test.com", "U2", "User");
        u2.LastOrdered = now.AddMinutes(-5);
        var u3 = CreateUser("u3", "u3@test.com", "U3", "User");
        u3.LastOrdered = now.AddMinutes(-10);
        
        await SaveUsers(u1, u2, u3);
        await AddUserToRole(u1, "Consumer");
        await AddUserToRole(u2, "Consumer");
        await AddUserToRole(u3, "Consumer");

        // Act
        var result = await _sut.GetAllPaged(1, 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].UserName.Should().Be("u2");
    }
    
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
    
    #region GetByIds Tests

    [TestMethod]
    public async Task GetByIds_WithKnownIds_ReturnsMatchingUsers()
    {
        // Arrange
        var user1 = CreateUser("byids1", "byids1@test.com", "User", "One");
        var user2 = CreateUser("byids2", "byids2@test.com", "User", "Two");
        var user3 = CreateUser("byids3", "byids3@test.com", "User", "Three");
        await SaveUsers(user1, user2, user3);

        // Act
        var result = await _sut.GetByIds(new[] { user1.Id, user3.Id });

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.UserName == "byids1");
        result.Should().Contain(u => u.UserName == "byids3");
        result.Should().NotContain(u => u.UserName == "byids2");
    }

    [TestMethod]
    public async Task GetByIds_WithEmptyIdList_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetByIds(Array.Empty<string>());

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetByIds_WithUnknownIds_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetByIds(new[] { Guid.NewGuid().ToString() });

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
    
    #region GetTopActiveUserIds Tests
    
    [TestMethod]
    public async Task GetTopActiveUserIds_WithMoreUsersThanTake_ReturnsMostRecentlyActiveIds()
    {
        // Arrange
        await SetupRolesAndUserRoles();
        
        var users = new List<ApplicationUser>();
        var product = await CreateProduct("Test Product", 10.0m);
        
        for (int i = 0; i < 15; i++)
        {
            string username = $"activeuser{i}";
            var user = CreateUser(username, $"{username}@test.com", "User", $"{i}");
            user.LastOrdered = DateTime.Now.AddHours(-i); // Different LastOrdered dates
            users.Add(user);
        }
        
        await SaveUsers(users.ToArray());
        
        foreach (var user in users)
        {
            await AddUserToRole(user, "Consumer");
            await CreateOrder(user, product);
        }
        
        // Act
        var result = await _sut.GetTopActiveUserIds(10);
        
        // Assert
        result.Should().HaveCount(10);
        
        var expectedIds = users.Take(10).Select(u => u.Id).ToList();
        foreach (var expectedId in expectedIds)
        {
            result.Should().Contain(expectedId);
        }
        
        var excludedIds = users.Skip(10).Select(u => u.Id).ToList();
        foreach (var excludedId in excludedIds)
        {
            result.Should().NotContain(excludedId);
        }
    }
    
    [TestMethod]
    public async Task GetTopActiveUserIds_WithFewerUsersThanTake_ReturnsAllIds()
    {
        // Arrange
        await SetupRolesAndUserRoles();
        
        var users = new List<ApplicationUser>();
        var product = await CreateProduct("Test Product", 10.0m);
        
        for (int i = 0; i < 5; i++)
        {
            string username = $"activeuser{i}";
            var user = CreateUser(username, $"{username}@test.com", "User", $"{i}");
            user.LastOrdered = DateTime.Now.AddHours(-i);
            users.Add(user);
        }
        
        await SaveUsers(users.ToArray());
        
        foreach (var user in users)
        {
            await AddUserToRole(user, "Consumer");
            await CreateOrder(user, product);
        }
        
        // Act
        var result = await _sut.GetTopActiveUserIds(10);
        
        // Assert
        result.Should().HaveCount(5);
        foreach (var expectedUser in users)
        {
            result.Should().Contain(expectedUser.Id);
        }
    }
    
    [TestMethod]
    public async Task GetTopActiveUserIds_WithNoUsersWithOrders_ReturnsEmptyList()
    {
        // Arrange
        await DeleteAllUsersAndRoles();
        await SetupRolesAndUserRoles();
        
        // Act
        var result = await _sut.GetTopActiveUserIds(10);
        
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetByIds(new[] { "testid" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetTopActiveUserIds(10));
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

