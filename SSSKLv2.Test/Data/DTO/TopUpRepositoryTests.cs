using Bogus;
using Bogus.DataSets;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SSSKLv2.Data;
using SSSKLv2.Test.Util;

namespace SSSKLv2.Test.Data.DTO;

[TestClass]
public class TopUpRepositoryTests : RepositoryTest
{
    private IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    [TestInitialize]
    public void TestInitialize()
    {
        InitializeDatabase();
        _dbContextFactory = GetContextFactory();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanupDatabase();
    }
}