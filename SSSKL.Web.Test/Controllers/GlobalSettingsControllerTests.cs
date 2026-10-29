using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SSSKL.Application.Features.Settings.Commands.UpdateGlobalSetting;
using SSSKL.Application.Features.Settings.Queries.GetGlobalSetting;
using SSSKL.Contracts.Dto.Api.v1;
using SSSKL.Domain.Constants;
using SSSKL.Web.Controllers.v1;

namespace SSSKL.Web.Test.Controllers;

[TestClass]
public class GlobalSettingsControllerTests
{
    private ISender _sender = null!;
    private GlobalSettingsController _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sender = Substitute.For<ISender>();
        _sut = new GlobalSettingsController(_sender)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [TestMethod]
    public async Task GetSetting_PassesAdminFlagAndReturnsOk()
    {
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, Roles.Admin)], "TestAuth"));
        var dto = new GlobalSettingDto { Key = "k", Value = "v" };
        _sender.Send(new GetGlobalSettingQuery("k", true), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.GetSetting("k");

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(dto);
    }

    [TestMethod]
    public async Task UpdateSetting_SendsCommandAndReturnsNoContent()
    {
        var result = await _sut.UpdateSetting("k", new GlobalSettingUpdateDto { Value = "v" });

        result.Should().BeOfType<NoContentResult>();
        await _sender.Received(1).Send(new UpdateGlobalSettingCommand("k", "v"), Arg.Any<CancellationToken>());
    }
}
