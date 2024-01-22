using Bogus;
using Bogus.DataSets;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DAL;

[TestClass]
public class TopUpRepositoryTests : RepositoryTest
{
    private MockDbContextFactory _dbContextFactory = null!;
    private TopUpRepository _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = new MockDbContextFactory(GetOptions());
        _sut = new TopUpRepository(_dbContextFactory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }
}