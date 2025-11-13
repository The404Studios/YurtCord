using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YurtCord.Application.Services;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;
using YurtCord.Infrastructure.Data;

namespace YurtCord.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(YurtCordDbContext context, IAuthService authService) : ControllerBase
{
    private readonly YurtCordDbContext _context = context;
    private readonly IAuthService _authService = authService;

    private async Task<Snowflake?> GetCurrentUserIdAsync()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var user = await _authService.GetUserFromTokenAsync(token);
        return user?.Id;
    }

    /// <summary>
    /// Get a user by ID
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        if (!Snowflake.TryParse(userId, out var snowflake))
            return BadRequest(new { error = "Invalid user ID" });

        var user = await _context.Users
            .Include(u => u.Presence)
            .FirstOrDefaultAsync(u => u.Id == snowflake);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            id = user.Id.ToString(),
            username = user.Username,
            discriminator = user.Discriminator,
            tag = user.Tag,
            avatar = user.Avatar,
            banner = user.Banner,
            accentColor = user.AccentColor,
            bio = user.Bio,
            flags = user.Flags.ToString(),
            premiumType = user.PremiumType.ToString(),
            publicFlags = user.PublicFlags.ToString(),
            createdAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Get current user (authenticated)
    /// </summary>
    [HttpGet("@me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Presence)
            .FirstOrDefaultAsync(u => u.Id == userId.Value);

        if (user == null)
            return Unauthorized();

        return Ok(new
        {
            id = user.Id.ToString(),
            username = user.Username,
            discriminator = user.Discriminator,
            tag = user.Tag,
            email = user.Email,
            avatar = user.Avatar,
            banner = user.Banner,
            accentColor = user.AccentColor,
            bio = user.Bio,
            verified = user.Verified,
            mfaEnabled = user.MfaEnabled,
            flags = user.Flags.ToString(),
            premiumType = user.PremiumType.ToString(),
            publicFlags = user.PublicFlags.ToString(),
            createdAt = user.CreatedAt,
            presence = user.Presence != null ? new
            {
                status = user.Presence.Status.ToString(),
                customStatus = user.Presence.CustomStatus,
                updatedAt = user.Presence.UpdatedAt
            } : null
        });
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    [HttpPatch("@me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserDto dto)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        // Update fields
        if (dto.Username != null && dto.Username.Length >= 2 && dto.Username.Length <= 32)
        {
            // Check if username is taken with same discriminator
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == dto.Username && u.Discriminator == user.Discriminator && u.Id != user.Id);

            if (existingUser != null)
                return BadRequest(new { error = "Username already taken" });

            user.Username = dto.Username;
        }

        if (dto.Avatar != null)
            user.Avatar = dto.Avatar;

        if (dto.Banner != null)
            user.Banner = dto.Banner;

        if (dto.AccentColor.HasValue)
            user.AccentColor = dto.AccentColor.Value;

        if (dto.Bio != null && dto.Bio.Length <= 190)
            user.Bio = dto.Bio;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = user.Id.ToString(),
            username = user.Username,
            discriminator = user.Discriminator,
            email = user.Email,
            avatar = user.Avatar,
            banner = user.Banner,
            accentColor = user.AccentColor,
            bio = user.Bio
        });
    }

    /// <summary>
    /// Get current user's guilds
    /// </summary>
    [HttpGet("@me/guilds")]
    public async Task<IActionResult> GetCurrentUserGuilds()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var guilds = await _context.GuildMembers
            .Where(gm => gm.UserId == userId.Value)
            .Include(gm => gm.Guild)
            .Select(gm => gm.Guild)
            .ToListAsync();

        return Ok(guilds.Select(g => new
        {
            id = g.Id.ToString(),
            name = g.Name,
            icon = g.Icon,
            banner = g.Banner,
            owner = g.OwnerId == userId.Value,
            permissions = "0", // Calculate actual permissions
            features = g.Features.ToString()
        }));
    }

    /// <summary>
    /// Leave a guild
    /// </summary>
    [HttpDelete("@me/guilds/{guildId}")]
    public async Task<IActionResult> LeaveGuild(string guildId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(guildId, out var guildSnowflake))
            return BadRequest(new { error = "Invalid guild ID" });

        var guild = await _context.Guilds.FindAsync(guildSnowflake);
        if (guild == null)
            return NotFound();

        // Can't leave if owner
        if (guild.OwnerId == userId.Value)
            return BadRequest(new { error = "You cannot leave a guild you own. Transfer ownership first." });

        var member = await _context.GuildMembers
            .FirstOrDefaultAsync(gm => gm.GuildId == guildSnowflake && gm.UserId == userId.Value);

        if (member == null)
            return NotFound();

        _context.GuildMembers.Remove(member);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get current user's DM channels
    /// </summary>
    [HttpGet("@me/channels")]
    public async Task<IActionResult> GetDMChannels()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        // This would need a proper DM channel implementation
        // For now, return empty array
        var channels = await _context.Channels
            .Where(c => c.Type == ChannelType.DM || c.Type == ChannelType.GroupDM)
            .ToListAsync();

        return Ok(channels.Select(c => new
        {
            id = c.Id.ToString(),
            type = c.Type.ToString(),
            lastMessageAt = c.LastMessageAt,
            recipients = new object[] { }
        }));
    }

    /// <summary>
    /// Create a DM channel
    /// </summary>
    [HttpPost("@me/channels")]
    public async Task<IActionResult> CreateDMChannel([FromBody] CreateDMDto dto)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(dto.RecipientId, out var recipientSnowflake))
            return BadRequest(new { error = "Invalid recipient ID" });

        var recipient = await _context.Users.FindAsync(recipientSnowflake);
        if (recipient == null)
            return NotFound(new { error = "User not found" });

        // Check if DM already exists
        // This would need proper DM channel tracking
        // For now, create a new channel

        var channel = new Channel
        {
            Id = new Snowflake(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
            Type = ChannelType.DM,
            Name = $"{userId}-{recipientSnowflake}",
            CreatedAt = DateTime.UtcNow
        };

        _context.Channels.Add(channel);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ChannelsController.GetChannel),
            "Channels",
            new { channelId = channel.Id.ToString() },
            new
            {
                id = channel.Id.ToString(),
                type = channel.Type.ToString(),
                recipients = new[]
                {
                    new
                    {
                        id = recipient.Id.ToString(),
                        username = recipient.Username,
                        discriminator = recipient.Discriminator,
                        avatar = recipient.Avatar
                    }
                }
            });
    }

    /// <summary>
    /// Update user presence status
    /// </summary>
    [HttpPatch("@me/presence")]
    public async Task<IActionResult> UpdatePresence([FromBody] UpdatePresenceDto dto)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var presence = await _context.UserPresences
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        if (presence == null)
        {
            presence = new UserPresence
            {
                UserId = userId.Value,
                Status = PresenceStatus.Online,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserPresences.Add(presence);
        }

        if (dto.Status != null && Enum.TryParse<PresenceStatus>(dto.Status, true, out var status))
            presence.Status = status;

        if (dto.CustomStatus != null)
            presence.CustomStatus = dto.CustomStatus;

        presence.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            status = presence.Status.ToString(),
            customStatus = presence.CustomStatus,
            updatedAt = presence.UpdatedAt
        });
    }

    /// <summary>
    /// Get user's connections (social links)
    /// </summary>
    [HttpGet("@me/connections")]
    public IActionResult GetConnections()
    {
        // Return empty array for now
        // Would implement social connections (Twitter, YouTube, etc.)
        return Ok(new object[] { });
    }
}

public record UpdateUserDto(
    string? Username = null,
    string? Avatar = null,
    string? Banner = null,
    int? AccentColor = null,
    string? Bio = null
);

public record CreateDMDto(string RecipientId);

public record UpdatePresenceDto(
    string? Status = null,
    string? CustomStatus = null
);
