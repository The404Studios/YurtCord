using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YurtCord.Application.Services;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;
using YurtCord.Infrastructure.Data;

namespace YurtCord.API.Controllers;

[ApiController]
[Route("api/channels")]
[Authorize]
public class ChannelsController(
    YurtCordDbContext context,
    IAuthService authService,
    IPermissionService permissionService) : ControllerBase
{
    private readonly YurtCordDbContext _context = context;
    private readonly IAuthService _authService = authService;
    private readonly IPermissionService _permissionService = permissionService;

    private async Task<Snowflake?> GetCurrentUserIdAsync()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var user = await _authService.GetUserFromTokenAsync(token);
        return user?.Id;
    }

    /// <summary>
    /// Get a channel by ID
    /// </summary>
    [HttpGet("{channelId}")]
    public async Task<IActionResult> GetChannel(string channelId)
    {
        if (!Snowflake.TryParse(channelId, out var snowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        var channel = await _context.Channels
            .Include(c => c.Guild)
            .Include(c => c.PermissionOverwrites)
            .FirstOrDefaultAsync(c => c.Id == snowflake);

        if (channel == null)
            return NotFound();

        return Ok(new
        {
            id = channel.Id.ToString(),
            type = channel.Type.ToString(),
            guildId = channel.GuildId?.ToString(),
            position = channel.Position,
            name = channel.Name,
            topic = channel.Topic,
            nsfw = channel.Nsfw,
            lastMessageAt = channel.LastMessageAt,
            bitrate = channel.Bitrate,
            userLimit = channel.UserLimit,
            rateLimitPerUser = channel.RateLimitPerUser,
            parentId = channel.ParentId?.ToString()
        });
    }

    /// <summary>
    /// Update a channel
    /// </summary>
    [HttpPatch("{channelId}")]
    public async Task<IActionResult> UpdateChannel(
        string channelId,
        [FromBody] UpdateChannelDto dto)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(channelId, out var snowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        var channel = await _context.Channels
            .Include(c => c.Guild)
            .FirstOrDefaultAsync(c => c.Id == snowflake);

        if (channel == null)
            return NotFound();

        // Check permissions
        if (channel.GuildId != null)
        {
            var hasPermission = await _permissionService.HasPermissionAsync(
                userId.Value,
                channel.GuildId.Value,
                Permissions.ManageChannels);

            if (!hasPermission)
                return Forbid();
        }

        // Update fields
        if (dto.Name != null)
            channel.Name = dto.Name;

        if (dto.Topic != null)
            channel.Topic = dto.Topic;

        if (dto.Position.HasValue)
            channel.Position = dto.Position.Value;

        if (dto.Nsfw.HasValue)
            channel.Nsfw = dto.Nsfw.Value;

        if (dto.RateLimitPerUser.HasValue)
            channel.RateLimitPerUser = dto.RateLimitPerUser.Value;

        if (dto.Bitrate.HasValue)
            channel.Bitrate = dto.Bitrate.Value;

        if (dto.UserLimit.HasValue)
            channel.UserLimit = dto.UserLimit.Value;

        if (dto.ParentId != null && Snowflake.TryParse(dto.ParentId, out var parentSnowflake))
            channel.ParentId = parentSnowflake;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = channel.Id.ToString(),
            type = channel.Type.ToString(),
            guildId = channel.GuildId?.ToString(),
            position = channel.Position,
            name = channel.Name,
            topic = channel.Topic,
            nsfw = channel.Nsfw,
            bitrate = channel.Bitrate,
            userLimit = channel.UserLimit,
            rateLimitPerUser = channel.RateLimitPerUser,
            parentId = channel.ParentId?.ToString()
        });
    }

    /// <summary>
    /// Delete a channel
    /// </summary>
    [HttpDelete("{channelId}")]
    public async Task<IActionResult> DeleteChannel(string channelId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(channelId, out var snowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        var channel = await _context.Channels
            .Include(c => c.Guild)
            .FirstOrDefaultAsync(c => c.Id == snowflake);

        if (channel == null)
            return NotFound();

        // Check permissions
        if (channel.GuildId != null)
        {
            var hasPermission = await _permissionService.HasPermissionAsync(
                userId.Value,
                channel.GuildId.Value,
                Permissions.ManageChannels);

            if (!hasPermission)
                return Forbid();
        }

        _context.Channels.Remove(channel);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get channel permission overwrites
    /// </summary>
    [HttpGet("{channelId}/permissions")]
    public async Task<IActionResult> GetChannelPermissions(string channelId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(channelId, out var snowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        var channel = await _context.Channels
            .Include(c => c.PermissionOverwrites)
            .FirstOrDefaultAsync(c => c.Id == snowflake);

        if (channel == null)
            return NotFound();

        // Calculate user's permissions for this channel
        var permissions = await _permissionService.CalculateChannelPermissionsAsync(
            userId.Value,
            snowflake);

        return Ok(new
        {
            permissions = permissions.ToString(),
            permissionsValue = (long)permissions,
            overwrites = channel.PermissionOverwrites.Select(po => new
            {
                id = po.TargetId.ToString(),
                type = po.Type.ToString(),
                allow = po.Allow.ToString(),
                deny = po.Deny.ToString()
            })
        });
    }

    /// <summary>
    /// Trigger typing indicator
    /// </summary>
    [HttpPost("{channelId}/typing")]
    public async Task<IActionResult> TriggerTyping(string channelId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(channelId, out var snowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        var channel = await _context.Channels.FindAsync(snowflake);
        if (channel == null)
            return NotFound();

        // In a real implementation, this would broadcast via SignalR
        // For now, just return 204

        return NoContent();
    }

    /// <summary>
    /// Get pinned messages in a channel
    /// </summary>
    [HttpGet("{channelId}/pins")]
    public async Task<IActionResult> GetPinnedMessages(string channelId)
    {
        if (!Snowflake.TryParse(channelId, out var snowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        var messages = await _context.Messages
            .Where(m => m.ChannelId == snowflake && m.Pinned)
            .Include(m => m.Author)
            .OrderByDescending(m => m.Timestamp)
            .Take(50)
            .ToListAsync();

        return Ok(messages.Select(m => new
        {
            id = m.Id.ToString(),
            channelId = m.ChannelId.ToString(),
            author = new
            {
                id = m.Author.Id.ToString(),
                username = m.Author.Username,
                discriminator = m.Author.Discriminator,
                avatar = m.Author.Avatar
            },
            content = m.Content,
            timestamp = m.Timestamp,
            editedTimestamp = m.EditedTimestamp
        }));
    }

    /// <summary>
    /// Pin a message
    /// </summary>
    [HttpPut("{channelId}/pins/{messageId}")]
    public async Task<IActionResult> PinMessage(string channelId, string messageId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        if (!Snowflake.TryParse(messageId, out var messageSnowflake))
            return BadRequest(new { error = "Invalid message ID" });

        var channel = await _context.Channels.FindAsync(channelSnowflake);
        if (channel == null)
            return NotFound();

        // Check permissions
        if (channel.GuildId != null)
        {
            var hasPermission = await _permissionService.HasChannelPermissionAsync(
                userId.Value,
                channelSnowflake,
                Permissions.ManageMessages);

            if (!hasPermission)
                return Forbid();
        }

        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageSnowflake && m.ChannelId == channelSnowflake);

        if (message == null)
            return NotFound();

        message.Pinned = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Unpin a message
    /// </summary>
    [HttpDelete("{channelId}/pins/{messageId}")]
    public async Task<IActionResult> UnpinMessage(string channelId, string messageId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        if (!Snowflake.TryParse(messageId, out var messageSnowflake))
            return BadRequest(new { error = "Invalid message ID" });

        var channel = await _context.Channels.FindAsync(channelSnowflake);
        if (channel == null)
            return NotFound();

        // Check permissions
        if (channel.GuildId != null)
        {
            var hasPermission = await _permissionService.HasChannelPermissionAsync(
                userId.Value,
                channelSnowflake,
                Permissions.ManageMessages);

            if (!hasPermission)
                return Forbid();
        }

        var message = await _context.Messages
            .FirstOrDefaultAsync(m => m.Id == messageSnowflake && m.ChannelId == channelSnowflake);

        if (message == null)
            return NotFound();

        message.Pinned = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record UpdateChannelDto(
    string? Name = null,
    string? Topic = null,
    int? Position = null,
    bool? Nsfw = null,
    int? RateLimitPerUser = null,
    int? Bitrate = null,
    int? UserLimit = null,
    string? ParentId = null
);
