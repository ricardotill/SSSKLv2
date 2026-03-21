using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SSSKLv2.Components.Account;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Exceptions;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class OrderControllerTests
{
    private IOrderService _orderService = null!;
    private ILogger<OrderController> _logger = null!;
    private IProductService _productService = null!;
    private IApplicationUserService _applicationUserService = null!;
    private OrderController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _orderService = Substitute.For<IOrderService>();
        _logger = Substitute.For<ILogger<OrderController>>();
        _productService = Substitute.For<IProductService>();
        _applicationUserService = Substitute.For<IApplicationUserService>();

        _sut = new OrderController(_orderService, _logger, _productService, _applicationUserService);
    }

    [TestMethod]
    public async Task Delete_AdminUser_DeletesAnyOrder()
    {
        // Arrange
        var id = Guid.NewGuid();
        SetUserContext(Guid.NewGuid().ToString(), "admin", isAdmin: true);

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _orderService.Received(1).DeleteOrder(id);
        await _orderService.DidNotReceive().GetOrderById(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task Delete_NonAdminOwner_DeletesOwnOrder()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString();
        var username = "user1";
        SetUserContext(userId, username, isAdmin: false);
        _orderService.GetOrderById(id).Returns(Task.FromResult(new Order
        {
            Id = id,
            User = new ApplicationUser { Id = userId, UserName = username },
            ProductNaam = "Test",
            Amount = 1,
            Paid = 1m
        }));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        await _orderService.Received(1).GetOrderById(id);
        await _orderService.Received(1).DeleteOrder(id);
    }

    [TestMethod]
    public async Task Delete_NonAdminNonOwner_ReturnsForbid()
    {
        // Arrange
        var id = Guid.NewGuid();
        SetUserContext(Guid.NewGuid().ToString(), "user1", isAdmin: false);
        _orderService.GetOrderById(id).Returns(Task.FromResult(new Order
        {
            Id = id,
            User = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = "user2" },
            ProductNaam = "Test",
            Amount = 1,
            Paid = 1m
        }));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
        await _orderService.Received(1).GetOrderById(id);
        await _orderService.DidNotReceive().DeleteOrder(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task Delete_NonAdminWithoutUserId_ReturnsUnauthorized()
    {
        // Arrange
        var id = Guid.NewGuid();
        SetUserContext(null, "user1", isAdmin: false);

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
        await _orderService.DidNotReceive().GetOrderById(Arg.Any<Guid>());
        await _orderService.DidNotReceive().DeleteOrder(Arg.Any<Guid>());
    }

    [TestMethod]
    public async Task Delete_NonAdminOrderNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        SetUserContext(Guid.NewGuid().ToString(), "user1", isAdmin: false);
        _orderService.GetOrderById(id).Returns(Task.FromException<Order>(new NotFoundException("Order not found")));

        // Act
        var result = await _sut.Delete(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        await _orderService.Received(1).GetOrderById(id);
        await _orderService.DidNotReceive().DeleteOrder(Arg.Any<Guid>());
    }

    private void SetUserContext(string? userId, string? username, bool isAdmin)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            claims.Add(new Claim(IdentityClaim.Id.ToString(), userId));
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            claims.Add(new Claim(ClaimTypes.Name, username));
        }

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }
}


