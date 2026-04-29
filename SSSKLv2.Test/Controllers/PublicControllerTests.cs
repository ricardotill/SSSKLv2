using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using System.Text.RegularExpressions;

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
        var mockEnv = Substitute.For<IWebHostEnvironment>();
        _mockConfiguration["WEBSITE_DOMAIN"].Returns("custom.domain.nl");
 
        // Act
        var result = _sut.GetDomain(mockEnv);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("custom.domain.nl");
    }

    [TestMethod]
    public void GetDomain_ReturnsDefaultDomain_WhenConfigMissing()
    {
        // Arrange
        var mockEnv = Substitute.For<IWebHostEnvironment>();
        mockEnv.EnvironmentName.Returns("Production");
        _mockConfiguration["WEBSITE_DOMAIN"].Returns((string?)null);
 
        // Act
        var result = _sut.GetDomain(mockEnv);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("ssskl.scoutingwilo.nl");
    }

    [TestMethod]
    public void GetDomain_ReturnsLocalhost_WhenConfigMissingInDevelopment()
    {
        // Arrange
        var mockEnv = Substitute.For<IWebHostEnvironment>();
        mockEnv.EnvironmentName.Returns("Development");
        _mockConfiguration["WEBSITE_DOMAIN"].Returns((string?)null);

        // Act
        var result = _sut.GetDomain(mockEnv);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be("localhost");
    }

    [TestMethod]
    public void GetVersion_ReturnsAssemblyVersion()
    {
        // Act
        var result = _sut.GetVersion();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var version = okResult.Value.Should().BeOfType<VersionDto>().Subject;
        version.Version.Should().Be("3.8.3");
    }

    [TestMethod]
    public void GetVersion_ReturnsSemVer()
    {
        // Act
        var result = _sut.GetVersion();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var version = okResult.Value.Should().BeOfType<VersionDto>().Subject;
        Regex.IsMatch(version.Version, @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$")
            .Should()
            .BeTrue();
    }
}
