using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Text.Json;
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
    private UserManager<ApplicationUser> _userManager = null!;
    private ApplicationUserController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _mockService = Substitute.For<IApplicationUserService>();
        var logger = Substitute.For<ILogger<ApplicationUserController>>();

        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = new UserManager<ApplicationUser>(
            userStore,
            Substitute.For<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<ILogger<UserManager<ApplicationUser>>>());

        _sut = new ApplicationUserController(_mockService, logger, _userManager);
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
            ProfilePictureUrl = null
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
            ProfilePictureUrl = null
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
            ProfilePictureUrl = null
        };
        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public async Task DownloadPersonalData_DoesNotIncludeAuthenticatorKey()
    {
        var user = new ApplicationUser
        {
            Id = "u-personal",
            UserName = "personal.user",
            Name = "Personal",
            Surname = "User",
            Saldo = 12.34m
        };

        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        var userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore,
            null,
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            Substitute.For<ILogger<UserManager<ApplicationUser>>>());

        userManager.GetUserAsync(Arg.Any<System.Security.Claims.ClaimsPrincipal>()).Returns(user);
        userManager.GetUserIdAsync(user).Returns(user.Id);
        var controller = new ApplicationUserController(_mockService, Substitute.For<ILogger<ApplicationUserController>>(), userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id)
                    }, "TestAuth"))
                }
            }
        };

        var result = await controller.DownloadPersonalData();

        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileDownloadName.Should().Be("PersonalData.json");
        fileResult.ContentType.Should().Be("application/json");

        var personalData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(fileResult.FileContents);
        personalData.Should().NotBeNull();
        personalData!.Should().ContainKey(nameof(ApplicationUser.Name));
        personalData.Should().ContainKey(nameof(ApplicationUser.Surname));
        personalData.Should().NotContainKey("Authenticator Key");

        _ = userManager.DidNotReceive().GetAuthenticatorKeyAsync(Arg.Any<ApplicationUser>());
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

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new ApplicationUserDetailedDto { Id = updatedUser.Id, UserName = updatedUser.UserName ?? string.Empty, Email = updatedUser.Email, FullName = updatedUser.FullName, Saldo = updatedUser.Saldo, LastOrdered = updatedUser.LastOrdered, ProfilePictureUrl = null, EmailConfirmed = updatedUser.EmailConfirmed, PhoneNumber = updatedUser.PhoneNumber, PhoneNumberConfirmed = updatedUser.PhoneNumberConfirmed, Name = updatedUser.Name, Surname = updatedUser.Surname });
    }

    [TestMethod]
    public async Task Update_WhenUserNameProvided_UserNameIsIgnored()
    {
        var id = "user1";
        var originalUserName = "oldname";
        var attemptedUserName = "newname";
        var dto = new ApplicationUserUpdateDto { Id = id, UserName = attemptedUserName, Email = "new@example.com" };
        var existing = new ApplicationUser { Id = id, UserName = originalUserName };
        var updatedUser = new ApplicationUser { Id = id, UserName = originalUserName, Email = dto.Email };

        _mockService.GetUserById(id).Returns(existing);
        _mockService.UpdateUser(id, Arg.Is<ApplicationUserUpdateDto>(d => d.UserName == null)).Returns(Task.FromResult(updatedUser));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin"), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin") }, "TestAuth"))
            }
        };

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<OkObjectResult>();
        // Verify service was called with null UserName (controller cleared it)
        await _mockService.Received(1).UpdateUser(id, Arg.Is<ApplicationUserUpdateDto>(d => d.UserName == null));
    }

    [TestMethod]
    public async Task Update_WhenRolesProvided_UpdatesUserRoles()
    {
        var id = "user1";
        var newRoles = new List<string> { "Admin", "Moderator" };
        var dto = new ApplicationUserUpdateDto { Id = id, Roles = newRoles };
        var existing = new ApplicationUser { Id = id, UserName = "user1" };
        var updatedUser = new ApplicationUser { Id = id, UserName = existing.UserName, Email = existing.Email };

        _mockService.GetUserById(id).Returns(existing);
        _mockService.UpdateUser(id, Arg.Is<ApplicationUserUpdateDto>(d => d.Roles == newRoles)).Returns(Task.FromResult(updatedUser));
        _mockService.GetUserRoles(id).Returns(Task.FromResult<IList<string>>(newRoles));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin"), new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin") }, "TestAuth"))
            }
        };

        var result = await _sut.Update(id, dto);

        result.Should().BeOfType<OkObjectResult>();
        await _mockService.Received(1).UpdateUser(id, Arg.Is<ApplicationUserUpdateDto>(d => d.Roles != null && d.Roles.Count == 2));
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

    [TestMethod]
    public async Task Delete_WhenSuccessful_ReturnsNoContent()
    {
        var id = "user1";

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NoContentResult>();
        await _mockService.Received(1).DeleteUser(id);
    }

    [TestMethod]
    public async Task Delete_WhenUserNotFound_ReturnsNotFound()
    {
        var id = "missing";
        _mockService.DeleteUser(id).Returns(Task.FromException(new NotFoundException("User not found")));

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task Delete_WhenServiceThrowsInvalidOperation_ReturnsBadRequestWithError()
    {
        var id = "user1";
        _mockService.DeleteUser(id).Returns(Task.FromException(new InvalidOperationException("Delete failed")));

        var result = await _sut.Delete(id);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task DeleteMe_WhenAuthenticated_ReturnsNoContent()
    {
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

        var result = await _sut.DeleteMe();

        result.Should().BeOfType<NoContentResult>();
        await _mockService.Received(1).GetUserByUsername(username);
        await _mockService.Received(1).DeleteUser(user.Id);
    }

    [TestMethod]
    public async Task DeleteMe_WhenUnauthenticated_ReturnsUnauthorized()
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity())
            }
        };

        var result = await _sut.DeleteMe();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public async Task DeleteMe_WhenCurrentUserNotFound_ReturnsNotFound()
    {
        var username = "missinguser";
        _mockService.GetUserByUsername(username).Returns(Task.FromException<ApplicationUser>(new NotFoundException("ApplicationUser not found")));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username) }, "TestAuth"))
            }
        };

        var result = await _sut.DeleteMe();

        result.Should().BeOfType<NotFoundResult>();
    }

    [TestMethod]
    public async Task DeleteMe_WhenDeleteFails_ReturnsBadRequestWithError()
    {
        var username = "currentuser";
        var user = new ApplicationUser { Id = "u-current", UserName = username };
        _mockService.GetUserByUsername(username).Returns(Task.FromResult(user));
        _mockService.DeleteUser(user.Id).Returns(Task.FromException(new InvalidOperationException("Delete failed")));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username) }, "TestAuth"))
            }
        };

        var result = await _sut.DeleteMe();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

}
