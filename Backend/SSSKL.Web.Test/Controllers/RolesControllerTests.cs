using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SSSKL.Application.Features.RoleManagement;
using SSSKL.Application.Features.RoleManagement.Commands.CreateRole;
using SSSKL.Application.Features.RoleManagement.Commands.DeleteRole;
using SSSKL.Application.Features.RoleManagement.Commands.UpdateRole;
using SSSKL.Application.Features.RoleManagement.Queries.GetRoles;
using SSSKL.Contracts.Dto.Api;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class RolesControllerTests
{
    private ISender _sender = null!;
    private RolesController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new RolesController(_sender);
    }

    [TestMethod]
    public async Task GetAll_ExcludesProtectedRoles()
    {
        IReadOnlyList<RoleDto> roles = [new RoleDto { Id = "1", Name = "CustomRole" }];
        _sender.Send(new GetRolesQuery(false), Arg.Any<CancellationToken>()).Returns(roles);

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(roles);
    }

    [TestMethod]
    public async Task GetAllAdmin_IncludesProtectedRoles()
    {
        IReadOnlyList<RoleDto> roles = [];
        _sender.Send(new GetRolesQuery(true), Arg.Any<CancellationToken>()).Returns(roles);

        var result = await _sut.GetAllAdmin();

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(roles);
    }

    [TestMethod]
    public async Task Create_WithValidName_ReturnsCreatedAtAction()
    {
        var role = new RoleDto { Id = "1", Name = "NewRole" };
        _sender.Send(new CreateRoleCommand("NewRole"), Arg.Any<CancellationToken>()).Returns(RoleResult.Success(role));

        var result = await _sut.Create(new CreateRoleDto { Name = "NewRole" });

        result.Result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().BeSameAs(role);
    }

    [TestMethod]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var result = await _sut.Create(new CreateRoleDto { Name = "" });

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await _sender.DidNotReceive().Send(Arg.Any<CreateRoleCommand>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Create_WhenIdentityRejects_ReturnsValidationProblem()
    {
        _sender.Send(Arg.Any<CreateRoleCommand>(), Arg.Any<CancellationToken>())
            .Returns(RoleResult.Failed(IdentityResult.Failed(new IdentityError { Code = "InvalidRoleName", Description = "Invalid" })));

        var result = await _sut.Create(new CreateRoleDto { Name = "x" });

        result.Result.Should().BeOfType<ObjectResult>().Which.Value.Should().BeOfType<ValidationProblemDetails>()
            .Which.Errors.Should().ContainKey("InvalidRoleName");
    }

    [TestMethod]
    public async Task Update_WithValidName_ReturnsOk()
    {
        var role = new RoleDto { Id = "1", Name = "NewName" };
        _sender.Send(new UpdateRoleCommand("1", "NewName"), Arg.Any<CancellationToken>()).Returns(RoleResult.Success(role));

        var result = await _sut.Update("1", new CreateRoleDto { Name = "NewName" });

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(role);
    }

    [TestMethod]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        _sender.Send(new DeleteRoleCommand("1"), Arg.Any<CancellationToken>()).Returns(RoleResult.Success(null));

        var result = await _sut.Delete("1");

        result.Should().BeOfType<NoContentResult>();
    }
}
