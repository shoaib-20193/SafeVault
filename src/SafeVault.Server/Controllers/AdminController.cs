using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SafeVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAdminData()
    {
        return Ok(new
        {
            message = "You have access to the administrator area."
        });
    }
}