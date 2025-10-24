using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Dto;
using SSSKLv2.Dto.Api.v1;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Data.DAL.Exceptions;
using Microsoft.Extensions.Logging;

namespace SSSKLv2.Test.Controllers;

[TestClass]
[DoNotParallelize]
public class ApplicationUserControllerTests
{
    private IApplicationUserService _mockService = null!;
    private ApplicationUserController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _mockService = Substitute.For<IApplicationUserService>();
        var logger = Substitute.For<ILogger<ApplicationUserController>>();
        _sut = new ApplicationUserController(_mockService, logger);
    }

    [TestMethod]
    public async Task GetById_WhenFound_ReturnsOk()
    {
        var id = "user1";
        var user = new ApplicationUser { Id = id, UserName = "u1" };
        _mockService.GetUserById(id).Returns(Task.FromResult(user));

        var result = await _sut.GetById(id);

        // ActionResult<T>.Result is the IActionResult (OkObjectResult/NotFoundResult)
        var expected = new ApplicationUserDetailedDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Name = user.Name,
            Surname = user.Surname,
            FullName = user.FullName,
            Saldo = user.Saldo,
            LastOrdered = user.LastOrdered,
            ProfilePictureBase64 = null
        };
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = "missing";
        _mockService.GetUserById(id).Returns(Task.FromException<ApplicationUser>(new NotFoundException("ApplicationUser not found")));

        var result = await _sut.GetById(id);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetByUsername_WhenFound_ReturnsOk()
    {
        var username = "john";
        var user = new ApplicationUser { Id = "u2", UserName = username };
        _mockService.GetUserByUsername(username).Returns(Task.FromResult(user));

        var result = await _sut.GetByUsername(username);

        var expected = new ApplicationUserDetailedDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Name = user.Name,
            Surname = user.Surname,
            FullName = user.FullName,
            Saldo = user.Saldo,
            LastOrdered = user.LastOrdered,
            ProfilePictureBase64 = null
        };
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task GetByUsername_WhenNotFound_ReturnsNotFound()
    {
        var username = "missinguser";
        _mockService.GetUserByUsername(username).Returns(Task.FromException<ApplicationUser>(new NotFoundException("ApplicationUser not found")));

        var result = await _sut.GetByUsername(username);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var list = new List<ApplicationUser>
        {
            new ApplicationUser { Id = "a", UserName = "a" },
            new ApplicationUser { Id = "b", UserName = "b" }
        };
        // Controller calls GetAllUsers(skip,take) and GetCount()
        _mockService.GetAllUsers(Arg.Any<int>(), Arg.Any<int>()).Returns(Task.FromResult((IList<ApplicationUser>)list));
        _mockService.GetCount().Returns(list.Count);

        var result = await _sut.GetAll();

        var expectedList = list.Select(u => new ApplicationUserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                FullName = u.FullName ?? string.Empty,
                Saldo = u.Saldo,
                LastOrdered = u.LastOrdered
            })
            .ToList();
        
        var expected = new PaginationObject<ApplicationUserDto>
        {
            Items = expectedList,
            TotalCount = list.Count
        };
        
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task GetAllObscured_ReturnsOk()
    {
        var list = new List<ApplicationUser>
        {
            new ApplicationUser { Id = "a", UserName = "a", PasswordHash = "*****" }
        };
        _mockService.GetAllUsersObscured().Returns(Task.FromResult(list.AsQueryable()));

        var result = await _sut.GetAllObscured();

        var expected = list.Select(u => new ApplicationUserDto { Id = u.Id, UserName = u.UserName ?? string.Empty, FullName = u.FullName ?? string.Empty, Saldo = u.Saldo, LastOrdered = u.LastOrdered }).ToList();
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task GetLeaderboard_ReturnsOkWithItems()
    {
        var productId = Guid.NewGuid();
        var entries = new List<LeaderboardEntryDto>
        {
            new LeaderboardEntryDto { Amount = 5, FullName = "User1", ProductName = "Product" }
        };
        _mockService.GetAllLeaderboard(productId).Returns(Task.FromResult((IEnumerable<LeaderboardEntryDto>)entries));

        var result = await _sut.GetLeaderboard(productId);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(entries);
    }

    [TestMethod]
    public async Task GetLeaderboard_WhenProductNotFound_ReturnsNotFound()
    {
        var productId = Guid.NewGuid();
        _mockService.GetAllLeaderboard(productId).Returns(Task.FromException<IEnumerable<LeaderboardEntryDto>>(new NotFoundException("Product not found")));

        var result = await _sut.GetLeaderboard(productId);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task GetCurrent_WhenAuthenticated_ReturnsOkWithUser()
    {
        // Arrange
        var username = "currentuser";
        var user = new ApplicationUser { Id = "u-current", UserName = username };
        _mockService.GetUserByUsername(username).Returns(Task.FromResult(user));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username) }, "TestAuth"))
            }
        };

        // Act
        var result = await _sut.GetCurrent();

        // Assert
        var expected = new ApplicationUserDetailedDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            Name = user.Name,
            Surname = user.Surname,
            FullName = user.FullName,
            Saldo = user.Saldo,
            LastOrdered = user.LastOrdered,
            ProfilePictureBase64 = null
        };
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task Update_AsAdmin_ReturnsOkWithUpdated()
    {
        var id = "user1";
        var dto = new ApplicationUserUpdateDto { Id = id, UserName = "newname", Email = "new@example.com" };
        var existing = new ApplicationUser { Id = id, UserName = "oldname" };
        var updatedUser = new ApplicationUser { Id = id, UserName = dto.UserName, Email = dto.Email };

        _mockService.GetUserById(id).Returns(existing);
        _mockService.UpdateUser(id, dto).Returns(Task.FromResult(updatedUser));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin"), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin") }, "TestAuth"))
            }
        };

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new ApplicationUserDetailedDto { Id = updatedUser.Id, UserName = updatedUser.UserName ?? string.Empty, Email = updatedUser.Email, FullName = updatedUser.FullName, Saldo = updatedUser.Saldo, LastOrdered = updatedUser.LastOrdered, ProfilePictureBase64 = null, EmailConfirmed = updatedUser.EmailConfirmed, PhoneNumber = updatedUser.PhoneNumber, PhoneNumberConfirmed = updatedUser.PhoneNumberConfirmed, Name = updatedUser.Name, Surname = updatedUser.Surname });
    }

    [TestMethod]
    public async Task Update_AsOwner_ReturnsOkWithUpdated()
    {
        var id = "user1";
        var username = "owneruser";
        var dto = new ApplicationUserUpdateDto { Id = id, UserName = "owneruser" };
        var existing = new ApplicationUser { Id = id, UserName = username };
        var updatedUser = new ApplicationUser { Id = id, UserName = username };

        _mockService.GetUserById(id).Returns(existing);
        _mockService.UpdateUser(id, dto).Returns(Task.FromResult(updatedUser));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username) }, "TestAuth"))
            }
        };

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [TestMethod]
    public async Task Update_WhenNotOwnerAndNotAdmin_ReturnsForbid()
    {
        var id = "user1";
        var dto = new ApplicationUserUpdateDto { Id = id, UserName = "someoneelse" };
        var existing = new ApplicationUser { Id = id, UserName = "otheruser" };

        _mockService.GetUserById(id).Returns(existing);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "notowner") }, "TestAuth"))
            }
        };

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<ForbidResult>();
    }

    [TestMethod]
    public async Task Update_WhenDtoNull_ReturnsBadRequest()
    {
        var id = "user1";
        var result = await _sut.Update(id, null);
        result.Should().BeOfType<BadRequestResult>();
    }

    [TestMethod]
    public async Task Update_WhenIdMismatch_ReturnsBadRequest()
    {
        var id = "user1";
        var dto = new ApplicationUserUpdateDto { Id = "different" };
        var result = await _sut.Update(id, dto);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task Update_WhenUserNotFound_ReturnsNotFound()
    {
        var id = "missing";
        var dto = new ApplicationUserUpdateDto { Id = id };
        _mockService.GetUserById(id).Returns(Task.FromException<ApplicationUser>(new NotFoundException("User not found")));

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Update_WhenServiceThrowsInvalidOperation_ReturnsBadRequestWithError()
    {
        var id = "user1";
        var dto = new ApplicationUserUpdateDto { Id = id };
        var existing = new ApplicationUser { Id = id, UserName = "user1" };
        _mockService.GetUserById(id).Returns(existing);
        _mockService.UpdateUser(id, dto).Returns(Task.FromException<ApplicationUser>(new InvalidOperationException("Bad update")));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, existing.UserName) }, "TestAuth"))
            }
        };

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

}
