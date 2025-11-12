using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YurtCord.Application.Services;
using YurtCord.Core.Common;

namespace YurtCord.API.Controllers;

[ApiController]
[Route("api/guilds")]
[Authorize]
public class GuildsController : ControllerBase
{
    private readonly IGuildService _guildService;
    private readonly IAuthService _authService;

    public GuildsController(IGuildService guildService, IAuthService authService)
    {
        _guildService = guildService;
        _authService = authService;
    }

    private async Task<Snowflake?> GetCurrentUserIdAsync()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var user = await _authService.GetUserFromTokenAsync(token);
        return user?.Id;
    }

    /// <summary>
    /// Get all guilds for the current user
    /// </summary>
    [HttpGet("@me")]
    public async Task<IActionResult> GetMyGuilds()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var guilds = await _guildService.GetUserGuildsAsync(userId.Value);

        return Ok(guilds.Select(g => new
        {
            id = g.Id.ToString(),
            name = g.Name,
            description = g.Description,
            icon = g.Icon,
            banner = g.Banner,
            ownerId = g.OwnerId.ToString(),
            memberCount = g.Members.Count
        }));
    }

    /// <summary>
    /// Get a specific guild by ID
    /// </summary>
    [HttpGet("{guildId}")]
    public async Task<IActionResult> GetGuild(string guildId)
    {
        if (!Snowflake.TryParse(guildId, out var snowflake))
            return BadRequest(new { error = "Invalid guild ID" });

        var guild = await _guildService.GetGuildAsync(snowflake);
        if (guild == null)
            return NotFound();

        return Ok(new
        {
            id = guild.Id.ToString(),
            name = guild.Name,
            description = guild.Description,
            icon = guild.Icon,
            banner = guild.Banner,
            splash = guild.Splash,
            ownerId = guild.OwnerId.ToString(),
            verificationLevel = guild.VerificationLevel.ToString(),
            features = guild.Features.ToString(),
            channels = guild.Channels.Select(c => new
            {
                id = c.Id.ToString(),
                type = c.Type.ToString(),
                name = c.Name,
                topic = c.Topic,
                position = c.Position
            }),
            roles = guild.Roles.Select(r => new
            {
                id = r.Id.ToString(),
                name = r.Name,
                color = r.Color,
                hoist = r.Hoist,
                position = r.Position,
                permissions = r.Permissions.ToString()
            })
        });
    }

    /// <summary>
    /// Create a new guild
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateGuild([FromBody] CreateGuildRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
            return BadRequest(new { error = "Guild name must be at least 2 characters" });

        var guild = await _guildService.CreateGuildAsync(request, userId.Value);

        return CreatedAtAction(nameof(GetGuild), new { guildId = guild!.Id.ToString() }, new
        {
            id = guild.Id.ToString(),
            name = guild.Name,
            description = guild.Description,
            ownerId = guild.OwnerId.ToString(),
            createdAt = guild.CreatedAt
        });
    }

    /// <summary>
    /// Update a guild
    /// </summary>
    [HttpPatch("{guildId}")]
    public async Task<IActionResult> UpdateGuild(string guildId, [FromBody] UpdateGuildRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(guildId, out var snowflake))
            return BadRequest(new { error = "Invalid guild ID" });

        var guild = await _guildService.UpdateGuildAsync(snowflake, request, userId.Value);
        if (guild == null)
            return NotFound();

        return Ok(new
        {
            id = guild.Id.ToString(),
            name = guild.Name,
            description = guild.Description,
            icon = guild.Icon,
            banner = guild.Banner
        });
    }

    /// <summary>
    /// Delete a guild
    /// </summary>
    [HttpDelete("{guildId}")]
    public async Task<IActionResult> DeleteGuild(string guildId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(guildId, out var snowflake))
            return BadRequest(new { error = "Invalid guild ID" });

        var success = await _guildService.DeleteGuildAsync(snowflake, userId.Value);
        if (!success)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Create a channel in a guild
    /// </summary>
    [HttpPost("{guildId}/channels")]
    public async Task<IActionResult> CreateChannel(string guildId, [FromBody] CreateChannelRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(guildId, out var snowflake))
            return BadRequest(new { error = "Invalid guild ID" });

        var channel = await _guildService.CreateChannelAsync(snowflake, request, userId.Value);
        if (channel == null)
            return Forbid();

        return CreatedAtAction("GetChannel", "Channels", new { channelId = channel.Id.ToString() }, new
        {
            id = channel.Id.ToString(),
            type = channel.Type.ToString(),
            name = channel.Name,
            topic = channel.Topic,
            position = channel.Position
        });
    }

    /// <summary>
    /// Create a role in a guild
    /// </summary>
    [HttpPost("{guildId}/roles")]
    public async Task<IActionResult> CreateRole(string guildId, [FromBody] CreateRoleRequest request)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(guildId, out var snowflake))
            return BadRequest(new { error = "Invalid guild ID" });

        var role = await _guildService.CreateRoleAsync(snowflake, request, userId.Value);
        if (role == null)
            return Forbid();

        return Ok(new
        {
            id = role.Id.ToString(),
            name = role.Name,
            color = role.Color,
            hoist = role.Hoist,
            position = role.Position,
            permissions = role.Permissions.ToString()
        });
    }
}
