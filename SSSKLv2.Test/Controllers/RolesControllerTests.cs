using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SSSKLv2.Controllers.v1;
using SSSKLv2.Data;
using SSSKLv2.Data.Constants;
using SSSKLv2.Dto.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSSKLv2.Test.Controllers;

[TestClass]
public class RolesControllerTests
{
    private RolesController _sut = null!;
    private RoleManager<IdentityRole> _roleManager = null!;
    private ApplicationDbContext _context = null!;
    private ILogger<RolesController> _mockLogger = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(dbContextOptions);
        
        var roleStore = new RoleStore<IdentityRole>(_context);
        var roleValidators = new List<IRoleValidator<IdentityRole>>();
        var lookupNormalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();
        var rmLogger = Substitute.For<ILogger<RoleManager<IdentityRole>>>();
        
        _roleManager = new RoleManager<IdentityRole>(roleStore, roleValidators, lookupNormalizer, describer, rmLogger);
        _mockLogger = Substitute.For<ILogger<RolesController>>();
        
        _sut = new RolesController(_roleManager, _mockLogger);
    }

    [TestMethod]
    public async Task GetAll_ReturnsOkWithNonProtectedRolesOnly()
    {
        // Arrange
        await _roleManager.CreateAsync(new IdentityRole(Roles.Admin)); // Protected
        await _roleManager.CreateAsync(new IdentityRole("CustomRole")); // Not protected
        
        // Act
        var result = await _sut.GetAll();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var roles = okResult.Value.Should().BeAssignableTo<IEnumerable<RoleDto>>().Subject;
        roles.Should().ContainSingle(r => r.Name == "CustomRole");
        roles.Should().NotContain(r => r.Name == Roles.Admin);
    }

    [TestMethod]
    public async Task GetAllAdmin_ReturnsOkWithAllRoles()
    {
        // Arrange
        await _roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        await _roleManager.CreateAsync(new IdentityRole("CustomRole"));
        
        // Act
        var result = await _sut.GetAllAdmin();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var roles = okResult.Value.Should().BeAssignableTo<IEnumerable<RoleDto>>().Subject;
        roles.Should().HaveCount(2);
        roles.Should().Contain(r => r.Name == Roles.Admin);
        roles.Should().Contain(r => r.Name == "CustomRole");
    }

    [TestMethod]
    public async Task Create_WithValidName_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateRoleDto { Name = "NewRole" };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var roleDto = createdResult.Value.Should().BeOfType<RoleDto>().Subject;
        roleDto.Name.Should().Be("NewRole");
        
        var roleExists = await _roleManager.RoleExistsAsync("NewRole");
        roleExists.Should().BeTrue();
    }

    [TestMethod]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateRoleDto { Name = "" };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [TestMethod]
    public async Task Create_WithDuplicateName_ReturnsConflict()
    {
        // Arrange
        await _roleManager.CreateAsync(new IdentityRole("ExistingRole"));
        var dto = new CreateRoleDto { Name = "ExistingRole" };

        // Act
        var result = await _sut.Create(dto);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [TestMethod]
    public async Task Update_WithValidName_ReturnsOk()
    {
        // Arrange
        var role = new IdentityRole("OldName");
        await _roleManager.CreateAsync(role);
        var dto = new CreateRoleDto { Name = "NewName" };

        // Act
        var result = await _sut.Update(role.Id, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var roleDto = okResult.Value.Should().BeOfType<RoleDto>().Subject;
        roleDto.Name.Should().Be("NewName");
        
        var updatedRole = await _roleManager.FindByIdAsync(role.Id);
        updatedRole!.Name.Should().Be("NewName");
    }

    [TestMethod]
    public async Task Update_ProtectedRole_ReturnsForbid()
    {
        // Arrange
        var role = new IdentityRole(Roles.Admin);
        await _roleManager.CreateAsync(role);
        var dto = new CreateRoleDto { Name = "NewName" };

        // Act
        var result = await _sut.Update(role.Id, dto);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [TestMethod]
    public async Task Delete_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var role = new IdentityRole("ToDelete");
        await _roleManager.CreateAsync(role);

        // Act
        var result = await _sut.Delete(role.Id);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var deletedRole = await _roleManager.FindByIdAsync(role.Id);
        deletedRole.Should().BeNull();
    }

    [TestMethod]
    public async Task Delete_ProtectedRole_ReturnsForbid()
    {
        // Arrange
        var role = new IdentityRole(Roles.Admin);
        await _roleManager.CreateAsync(role);

        // Act
        var result = await _sut.Delete(role.Id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [TestMethod]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        // Act
        var result = await _sut.Delete(Guid.NewGuid().ToString());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
