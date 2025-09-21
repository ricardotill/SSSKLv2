using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;

namespace SSSKLv2.Test.Util;

public abstract class RepositoryTest
{
    private DbConnection _connection;
    private DbContextOptions<ApplicationDbContext> _options;
    
    protected void InitializeDatabase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        
        using var context = new ApplicationDbContext(_options);
        context.Database.EnsureCreated();
        
        context.Users.Add(TestUser);
        context.Product.Add(TestProduct);
        context.SaveChanges();
    }

    public DbContextOptions<ApplicationDbContext> GetOptions() => _options;
    
    protected ApplicationUser TestUser { get; } = new ApplicationUser() {
        Id = Guid.NewGuid().ToString(),
        UserName = "testuser",
        Name = "Test",
        Surname = "User",
        Email = ""
    };
    
    protected Product TestProduct { get; } = new Product() {
        Id = Guid.NewGuid(),
        Name = "Test",
        Description = "Test Product",
        Price = 10.00m,
    };
    
    

    protected void CleanupDatabase()
    {
        _connection?.Dispose();
    }
}