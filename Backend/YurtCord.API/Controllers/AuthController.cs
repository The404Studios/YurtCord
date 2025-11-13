using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YurtCord.Application.Services;

namespace YurtCord.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 2)
            return BadRequest(new { error = "Username must be at least 2 characters" });

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(new { error = "Valid email is required" });

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { error = "Password must be at least 8 characters" });

        var result = await _authService.RegisterAsync(request);

        if (!result.Succeeded)
            return BadRequest(new { error = result.Error });

        return Ok(new
        {
            token = result.Token,
            user = new
            {
                id = result.User!.Id.ToString(),
                username = result.User.Username,
                discriminator = result.User.Discriminator,
                email = result.User.Email,
                avatar = result.User.Avatar,
                verified = result.User.Verified
            }
        });
    }

    /// <summary>
    /// Login to an existing account
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Email and password are required" });

        var result = await _authService.LoginAsync(request);

        if (!result.Succeeded)
            return Unauthorized(new { error = result.Error });

        return Ok(new
        {
            token = result.Token,
            user = new
            {
                id = result.User!.Id.ToString(),
                username = result.User.Username,
                discriminator = result.User.Discriminator,
                email = result.User.Email,
                avatar = result.User.Avatar,
                verified = result.User.Verified
            }
        });
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var user = await _authService.GetUserFromTokenAsync(token);

        if (user == null)
            return Unauthorized();

        return Ok(new
        {
            id = user.Id.ToString(),
            username = user.Username,
            discriminator = user.Discriminator,
            email = user.Email,
            avatar = user.Avatar,
            banner = user.Banner,
            bio = user.Bio,
            verified = user.Verified,
            mfaEnabled = user.MfaEnabled,
            flags = user.Flags.ToString(),
            premiumType = user.PremiumType.ToString(),
            createdAt = user.CreatedAt
        });
    }
}
