using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SSSKLv2.Services;
using SSSKLv2.Data;
using System.Net.Http;
using System.Reflection;

namespace SSSKLv2.Test.Services;

[TestClass]
public class WebPushServiceSanitizationTests
{
    private WebPushService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder().Build();
        var dbContext = Substitute.For<ApplicationDbContext>(new Microsoft.EntityFrameworkCore.DbContextOptions<ApplicationDbContext>());
        var logger = Substitute.For<ILogger<WebPushService>>();
        _service = new WebPushService(configuration, dbContext, new HttpClient(), logger);
    }

    [TestMethod]
    public void SanitizeTopic_ShouldRemoveInvalidCharacters()
    {
        // Arrange
        var title = "Nieuwe bestelling!";
        
        // Act
        var method = typeof(WebPushService).GetMethod("SanitizeTopic", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string?)method!.Invoke(_service, new object[] { title });

        // Assert
        result.Should().Be("nieuwe-bestelling");
    }

    [TestMethod]
    public void SanitizeTopic_ShouldAllowValidCharacters()
    {
        // Arrange
        var title = "valid.topic_with~tilde-and.more";
        
        // Act
        var method = typeof(WebPushService).GetMethod("SanitizeTopic", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string?)method!.Invoke(_service, new object[] { title });

        // Assert
        result.Should().Be("valid.topic_with~tilde-and.more");
    }

    [TestMethod]
    public void SanitizeTopic_ShouldTruncateTo32Chars()
    {
        // Arrange
        var title = "this-is-a-very-long-topic-that-definitely-exceeds-thirty-two-characters";
        
        // Act
        var method = typeof(WebPushService).GetMethod("SanitizeTopic", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string?)method!.Invoke(_service, new object[] { title });

        // Assert
        result!.Length.Should().Be(32);
        result.Should().Be("this-is-a-very-long-topic-that-d");
    }

    [TestMethod]
    public void SanitizeTopic_ShouldReturnNullForEmptyOrInvalidOnly()
    {
        // Arrange
        var title = "!!!";
        
        // Act
        var method = typeof(WebPushService).GetMethod("SanitizeTopic", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (string?)method!.Invoke(_service, new object[] { title });

        // Assert
        result.Should().BeNull();
    }
}
