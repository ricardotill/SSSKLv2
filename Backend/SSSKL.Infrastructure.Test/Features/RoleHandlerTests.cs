using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SSSKL.Application.Features.RoleManagement.Commands.CreateRole;
using SSSKL.Application.Features.RoleManagement.Commands.DeleteRole;
using SSSKL.Application.Features.RoleManagement.Commands.UpdateRole;
using SSSKL.Application.Features.RoleManagement.Queries.GetRoles;
using SSSKL.Domain.Constants;
using SSSKL.Domain.Exceptions;
using SSSKL.Infrastructure.Persistence;
using SSSKL.Infrastructure.Persistence.Repositories;

namespace SSSKL.Infrastructure.Test.Features;

[TestClass]
public class RoleHandlerTests
{
    private ApplicationDbContext _context = null!;
    private RoleManager<IdentityRole> _roleManager = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _roleManager = new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole>(_context),
            new List<IRoleValidator<IdentityRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Substitute.For<ILogger<RoleManager<IdentityRole>>>());
    }

    [TestCleanup]
    public void TestCleanup() => _context.Dispose();

    [TestMethod]
    public async Task GetRoles_WithoutProtected_ReturnsCustomRolesOnly()
    {
        await _roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        await _roleManager.CreateAsync(new IdentityRole("CustomRole"));

        var result = await new GetRolesHandler(new RoleRepository(_context)).Handle(new GetRolesQuery(IncludeProtected: false), CancellationToken.None);

        result.Should().ContainSingle(r => r.Name == "CustomRole");
        result.Should().NotContain(r => r.Name == Roles.Admin);
    }

    [TestMethod]
    public async Task GetRoles_WithProtected_ReturnsAllRoles()
    {
        await _roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        await _roleManager.CreateAsync(new IdentityRole("CustomRole"));

        var result = await new GetRolesHandler(new RoleRepository(_context)).Handle(new GetRolesQuery(IncludeProtected: true), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task CreateRole_WithValidName_CreatesRole()
    {
        var result = await new CreateRoleHandler(_roleManager).Handle(new CreateRoleCommand("NewRole"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Role!.Name.Should().Be("NewRole");
        (await _roleManager.RoleExistsAsync("NewRole")).Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateRole_WithDuplicateName_ThrowsConflict()
    {
        await _roleManager.CreateAsync(new IdentityRole("ExistingRole"));

        var act = () => new CreateRoleHandler(_roleManager).Handle(new CreateRoleCommand("ExistingRole"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [TestMethod]
    public async Task UpdateRole_WithValidName_RenamesRole()
    {
        var role = new IdentityRole("OldName");
        await _roleManager.CreateAsync(role);

        var result = await new UpdateRoleHandler(_roleManager).Handle(new UpdateRoleCommand(role.Id, "NewName"), CancellationToken.None);

        result.Role!.Name.Should().Be("NewName");
        (await _roleManager.FindByIdAsync(role.Id))!.Name.Should().Be("NewName");
    }

    [TestMethod]
    public async Task UpdateRole_ToOtherExistingName_ThrowsConflict()
    {
        var role = new IdentityRole("A");
        await _roleManager.CreateAsync(role);
        await _roleManager.CreateAsync(new IdentityRole("B"));

        var act = () => new UpdateRoleHandler(_roleManager).Handle(new UpdateRoleCommand(role.Id, "B"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [TestMethod]
    public async Task UpdateRole_ProtectedRole_ThrowsUnauthorized()
    {
        var role = new IdentityRole(Roles.Admin);
        await _roleManager.CreateAsync(role);

        var act = () => new UpdateRoleHandler(_roleManager).Handle(new UpdateRoleCommand(role.Id, "NewName"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task DeleteRole_WithValidId_DeletesRole()
    {
        var role = new IdentityRole("ToDelete");
        await _roleManager.CreateAsync(role);

        var result = await new DeleteRoleHandler(_roleManager).Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        (await _roleManager.FindByIdAsync(role.Id)).Should().BeNull();
    }

    [TestMethod]
    public async Task DeleteRole_ProtectedRole_ThrowsUnauthorized()
    {
        var role = new IdentityRole(Roles.Admin);
        await _roleManager.CreateAsync(role);

        var act = () => new DeleteRoleHandler(_roleManager).Handle(new DeleteRoleCommand(role.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [TestMethod]
    public async Task DeleteRole_NonExistent_ThrowsNotFound()
    {
        var act = () => new DeleteRoleHandler(_roleManager).Handle(new DeleteRoleCommand(Guid.NewGuid().ToString()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
