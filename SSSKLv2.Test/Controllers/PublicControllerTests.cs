using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using System.Threading.Tasks;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class PublicControllerTests
{
    private PublicController _sut = null!;
    private IConfiguration _mockConfiguration = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockConfiguration = Substitute.For<IConfiguration>();
        _sut = new PublicController(_mockConfiguration);
    }

    [TestMethod]
    public void GetDomain_ReturnsConfiguredDomain()
    {
        // Arrange
        _mockConfiguration["WEBSITE_DOMAIN"].Returns("custom.domain.nl");

        // Act
        var result = _sut.GetDomain();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("custom.domain.nl");
    }

    [TestMethod]
    public void GetDomain_ReturnsDefaultDomain_WhenConfigMissing()
    {
        // Arrange
        _mockConfiguration["WEBSITE_DOMAIN"].Returns((string?)null);

        // Act
        var result = _sut.GetDomain();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("ssskl.scoutingwilo.nl");
    }
}
