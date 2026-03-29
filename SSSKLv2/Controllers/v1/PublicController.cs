using Microsoft.AspNetCore.Mvc;

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
    public ActionResult<string> GetDomain()
    {
        var domain = _configuration["WEBSITE_DOMAIN"];
        return Ok(domain ?? "ssskl.scoutingwilo.nl");
    }
}
