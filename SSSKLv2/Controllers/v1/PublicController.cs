using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;

namespace SSSKLv2.Controllers.v1;

public sealed record VersionDto(string Version);

[Route("v1/[controller]")]
[ApiController]
public class PublicController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public PublicController(IConfiguration configuration)
    {
        _configuration = configuration; 
    }

    [HttpGet("domain")]
    public ActionResult<string> GetDomain([FromServices] IWebHostEnvironment env)
    {
        var domain = _configuration["WEBSITE_DOMAIN"];
        return Ok(domain ?? (env.IsDevelopment() ? "localhost" : "ssskl.scoutingwilo.nl"));
    }

    [HttpGet("version")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public ActionResult<VersionDto> GetVersion()
    {
        if (HttpContext is not null)
        {
            Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";
        }

        var version = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? "0.0.0";

        return Ok(new VersionDto(version));
    }
}
