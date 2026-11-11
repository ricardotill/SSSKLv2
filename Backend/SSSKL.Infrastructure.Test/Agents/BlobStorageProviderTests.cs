using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using SSSKL.Infrastructure.Agents;
using SSSKL.Infrastructure.Hosting;

namespace SSSKL.Infrastructure.Test.Agents;

[TestClass]
public class BlobStorageProviderTests
{
    private static bool UsesLocalFiles(string? provider, string environmentName)
    {
        var settings = new Dictionary<string, string?>();
        if (provider != null) settings[BlobStorageProvider.ConfigurationKey] = provider;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return BlobStorageProvider.UsesLocalFiles(configuration, environment);
    }

    [TestMethod]
    [DataRow("Production")]
    [DataRow("Development")]
    [DataRow(TestEnvironments.IntegrationTest)]
    public void UsesLocalFiles_WithoutProvider_IsFalseOutsideE2E(string environmentName)
    {
        UsesLocalFiles(provider: null, environmentName).Should().BeFalse();
    }

    [TestMethod]
    public void UsesLocalFiles_WithoutProvider_IsTrueInE2E()
    {
        UsesLocalFiles(provider: null, TestEnvironments.E2E).Should().BeTrue();
    }

    [TestMethod]
    [DataRow("LocalFile")]
    [DataRow("localfile")]
    public void UsesLocalFiles_LocalFileProvider_IsTrue(string provider)
    {
        UsesLocalFiles(provider, "Production").Should().BeTrue();
    }

    [TestMethod]
    [DataRow("Azure")]
    [DataRow("azure")]
    public void UsesLocalFiles_AzureProvider_IsFalseEvenInE2E(string provider)
    {
        UsesLocalFiles(provider, TestEnvironments.E2E).Should().BeFalse();
    }

    [TestMethod]
    public void UsesLocalFiles_UnknownProvider_Throws()
    {
        var act = () => UsesLocalFiles("S3", "Production");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Storage:Provider*S3*");
    }
}
