using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SafeVault.Server.DTOs.Auth;
using SafeVault.Server.Models;
using Microsoft.AspNetCore.Authorization;

namespace SafeVault.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser is not null)
        {
            return Conflict(new
            {
                message = "Unable to create account."
            });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Unable to create account.",
                errors = result.Errors.Select(error => error.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "User");

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Unable to complete account creation."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Account created successfully."
        });
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        return Ok(new
        {
            message = "Login successful."
        });
    }
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "You are not authenticated."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            roles,
            claims = User.Claims.Select(c => new
            {
                type = c.Type,
                value = c.Value
            })
        });
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return Ok(new
        {
            message = "Logout successful."
        });
    }
}