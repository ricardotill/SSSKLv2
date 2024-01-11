using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;

namespace SSSKLv2.Test.Util;

public class MockDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private DbContextOptions<ApplicationDbContext> _options;

    public MockDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    {
        _options = options;
    }

    public ApplicationDbContext CreateDbContext() => new(_options);
}