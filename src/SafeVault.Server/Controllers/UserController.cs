using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SafeVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "User")]
public class UserController : ControllerBase
{
    [HttpGet]
    public IActionResult GetUserData()
    {
        return Ok(new
        {
            message = "You have access to the user area."
        });
    }
}