using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class RssControllerTests
{
    private RssController _sut = null!;
    private IProductService _mockProductService = null!;
    private IAnnouncementService _mockAnnouncementService = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockProductService = Substitute.For<IProductService>();
        _mockAnnouncementService = Substitute.For<IAnnouncementService>();
        _sut = new RssController(_mockProductService, _mockAnnouncementService);
        
        // Mock HttpContext for Request.Scheme, Request.Host, etc.
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("test.host");
        httpContext.Request.PathBase = new PathString("/base");
        
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [TestMethod]
    public async Task GetProductsFeed_ReturnsRssFile()
    {
        // Arrange
        var products = new List<Product>
        {
            new Product { Name = "P1", Price = 1.5m, Stock = 10 },
            new Product { Name = "P2", Price = 2.0m, Stock = 5 }
        };
        _mockProductService.GetAllAvailable().Returns(products);

        // Act
        var result = await _sut.GetProductsFeed();

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = (FileContentResult)result;
        fileResult.ContentType.Should().Be("application/rss+xml; charset=utf-8");
        fileResult.FileContents.Should().NotBeEmpty();
        
        await _mockProductService.Received(1).GetAllAvailable();
    }

    [TestMethod]
    public async Task GetAnnouncementsFeed_ReturnsRssFile()
    {
        // Arrange
        var announcements = new List<Announcement>
        {
            new Announcement { Message = "M1", Description = "D1", Url = "https://link.com" },
            new Announcement { Message = "M2", Description = "D2" }
        };
        _mockAnnouncementService.GetAllAnnouncements().Returns(announcements);

        // Act
        var result = await _sut.GetAnnouncementsFeed();

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = (FileContentResult)result;
        fileResult.ContentType.Should().Be("application/rss+xml; charset=utf-8");
        fileResult.FileContents.Should().NotBeEmpty();
        
        await _mockAnnouncementService.Received(1).GetAllAnnouncements();
    }
}
