using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Moq;
using SSSKLv2.Data;

namespace SSSKLv2.Test.Util;

public abstract class RepositoryTest
{
    private DbConnection _connection;
    private DbContextOptions<ApplicationDbContext> _options;
    private Mock<IDbContextFactory<ApplicationDbContext>> _dbContextFactoryMock;

    protected void InitializeDatabase()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContextFactoryMock.Setup(f => f.CreateDbContextAsync(CancellationToken.None))
            .ReturnsAsync(() => new ApplicationDbContext(_options));
        
        using var context = new ApplicationDbContext(_options);
        context.Database.EnsureCreated();
    }

    public IDbContextFactory<ApplicationDbContext> GetContextFactory() => _dbContextFactoryMock.Object;

    protected void CleanupDatabase()
    {
        _connection?.Dispose();
    }
}