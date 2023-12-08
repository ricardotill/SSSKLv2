using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Test.Services;

[TestClass]
public class TopUpServiceTests
{
    private Mock<ITopUpRepository> _repository;

    [TestInitialize]
    public void Initialize()
    {
        _repository = new Mock<ITopUpRepository>(MockBehavior.Strict);
    }
}