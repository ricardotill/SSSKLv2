using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace SSSKLv2.Controllers.v1;

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
}
