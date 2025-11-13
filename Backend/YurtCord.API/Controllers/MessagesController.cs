using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YurtCord.Application.Services;
using YurtCord.Core.Common;

namespace YurtCord.API.Controllers;

[ApiController]
[Route("api/channels/{channelId}/messages")]
[Authorize]
public class MessagesController(IMessageService messageService, IAuthService authService) : ControllerBase
{
    private readonly IMessageService _messageService = messageService;
    private readonly IAuthService _authService = authService;

    private async Task<Snowflake?> GetCurrentUserIdAsync()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var user = await _authService.GetUserFromTokenAsync(token);
        return user?.Id;
    }

    /// <summary>
    /// Get messages in a channel
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMessages(
        string channelId,
        [FromQuery] int limit = 50,
        [FromQuery] string? before = null,
        [FromQuery] string? after = null)
    {
        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        Snowflake? beforeSnowflake = null;
        if (!string.IsNullOrEmpty(before) && Snowflake.TryParse(before, out var bs))
            beforeSnowflake = bs;

        Snowflake? afterSnowflake = null;
        if (!string.IsNullOrEmpty(after) && Snowflake.TryParse(after, out var as_))
            afterSnowflake = as_;

        var messages = await _messageService.GetChannelMessagesAsync(
            channelSnowflake,
            Math.Min(limit, 100),
            beforeSnowflake,
            afterSnowflake);

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
            editedTimestamp = m.EditedTimestamp,
            tts = m.Tts,
            mentionEveryone = m.MentionEveryone,
            mentions = m.Mentions.Select(u => new
            {
                id = u.User.Id.ToString(),
                username = u.User.Username,
                discriminator = u.User.Discriminator
            }),
            reactions = m.Reactions.GroupBy(r => r.EmojiName).Select(g => new
            {
                emoji = g.Key,
                count = g.Count(),
                me = g.Any(r => r.UserId == GetCurrentUserIdAsync().Result)
            }),
            type = m.Type.ToString()
        }));
    }

    /// <summary>
    /// Get a specific message
    /// </summary>
    [HttpGet("{messageId}")]
    public async Task<IActionResult> GetMessage(string channelId, string messageId)
    {
        if (!Snowflake.TryParse(messageId, out var messageSnowflake))
            return BadRequest(new { error = "Invalid message ID" });

        var message = await _messageService.GetMessageAsync(messageSnowflake);
        if (message == null)
            return NotFound();

        return Ok(new
        {
            id = message.Id.ToString(),
            channelId = message.ChannelId.ToString(),
            author = new
            {
                id = message.Author.Id.ToString(),
                username = message.Author.Username,
                discriminator = message.Author.Discriminator,
                avatar = message.Author.Avatar
            },
            content = message.Content,
            timestamp = message.Timestamp,
            editedTimestamp = message.EditedTimestamp,
            tts = message.Tts,
            mentionEveryone = message.MentionEveryone,
            mentions = message.Mentions.Select(u => new
            {
                id = u.User.Id.ToString(),
                username = u.User.Username,
                discriminator = u.User.Discriminator
            }),
            reactions = message.Reactions.GroupBy(r => r.EmojiName).Select(g => new
            {
                emoji = g.Key,
                count = g.Count()
            }),
            type = message.Type.ToString()
        });
    }

    /// <summary>
    /// Send a message to a channel
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateMessage(string channelId, [FromBody] CreateMessageDto dto)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 2000)
            return BadRequest(new { error = "Message content must be between 1 and 2000 characters" });

        Snowflake? referencedMessageId = null;
        if (!string.IsNullOrEmpty(dto.MessageReference) &&
            Snowflake.TryParse(dto.MessageReference, out var refId))
            referencedMessageId = refId;

        var request = new CreateMessageRequest(
            channelSnowflake,
            dto.Content,
            dto.Tts,
            referencedMessageId
        );

        var message = await _messageService.CreateMessageAsync(request, userId.Value);
        if (message == null)
            return Forbid();

        return CreatedAtAction(nameof(GetMessage),
            new { channelId = channelId, messageId = message.Id.ToString() },
            new
            {
                id = message.Id.ToString(),
                channelId = message.ChannelId.ToString(),
                author = new
                {
                    id = message.Author.Id.ToString(),
                    username = message.Author.Username,
                    discriminator = message.Author.Discriminator,
                    avatar = message.Author.Avatar
                },
                content = message.Content,
                timestamp = message.Timestamp,
                tts = message.Tts,
                mentionEveryone = message.MentionEveryone
            });
    }

    /// <summary>
    /// Edit a message
    /// </summary>
    [HttpPatch("{messageId}")]
    public async Task<IActionResult> EditMessage(
        string channelId,
        string messageId,
        [FromBody] EditMessageDto dto)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(messageId, out var messageSnowflake))
            return BadRequest(new { error = "Invalid message ID" });

        if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 2000)
            return BadRequest(new { error = "Message content must be between 1 and 2000 characters" });

        var message = await _messageService.EditMessageAsync(
            messageSnowflake,
            dto.Content,
            userId.Value);

        if (message == null)
            return Forbid();

        return Ok(new
        {
            id = message.Id.ToString(),
            channelId = message.ChannelId.ToString(),
            content = message.Content,
            editedTimestamp = message.EditedTimestamp
        });
    }

    /// <summary>
    /// Delete a message
    /// </summary>
    [HttpDelete("{messageId}")]
    public async Task<IActionResult> DeleteMessage(string channelId, string messageId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(messageId, out var messageSnowflake))
            return BadRequest(new { error = "Invalid message ID" });

        var success = await _messageService.DeleteMessageAsync(messageSnowflake, userId.Value);
        if (!success)
            return Forbid();

        return NoContent();
    }

    /// <summary>
    /// Add a reaction to a message
    /// </summary>
    [HttpPut("{messageId}/reactions/{emoji}/@me")]
    public async Task<IActionResult> AddReaction(string channelId, string messageId, string emoji)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(messageId, out var messageSnowflake))
            return BadRequest(new { error = "Invalid message ID" });

        var success = await _messageService.AddReactionAsync(
            messageSnowflake,
            userId.Value,
            emoji);

        if (!success)
            return BadRequest(new { error = "Could not add reaction" });

        return NoContent();
    }

    /// <summary>
    /// Remove a reaction from a message
    /// </summary>
    [HttpDelete("{messageId}/reactions/{emoji}/@me")]
    public async Task<IActionResult> RemoveReaction(string channelId, string messageId, string emoji)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(messageId, out var messageSnowflake))
            return BadRequest(new { error = "Invalid message ID" });

        var success = await _messageService.RemoveReactionAsync(
            messageSnowflake,
            userId.Value,
            emoji);

        if (!success)
            return NotFound();

        return NoContent();
    }
}

public record CreateMessageDto(string Content, bool Tts = false, string? MessageReference = null);
public record EditMessageDto(string Content);
