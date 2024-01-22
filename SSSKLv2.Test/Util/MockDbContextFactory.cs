using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;

namespace SSSKLv2.Test.Util;

public class MockDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public MockDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    {
        _options = options;
    }

    public ApplicationDbContext CreateDbContext() => new(_options);
    public Task<ApplicationDbContext> CreateDbContextAsync() => Task.Run(async delegate
    {
        await Task.Delay(10);
        return CreateDbContext();
    });
}