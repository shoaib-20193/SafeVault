using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SafeVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ITOnly")]
public class ITController : ControllerBase
{
    [HttpGet]
    public IActionResult GetITData()
    {
        return Ok(new
        {
            message = "You have access to the IT-only area."
        });
    }
}