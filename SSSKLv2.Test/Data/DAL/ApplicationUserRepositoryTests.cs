using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class ApplicationUserRepositoryTests : RepositoryTest
{
    private IDbContextFactory<ApplicationDbContext> _dbContextFactory = null!;
    private ApplicationUserRepository _sut;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = new MockDbContextFactory(GetOptions());
        _sut = new ApplicationUserRepository(_dbContextFactory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }
}