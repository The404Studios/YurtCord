using Microsoft.EntityFrameworkCore;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;
using YurtCord.Infrastructure.Data;

namespace YurtCord.Application.Services;

public interface IMessageService
{
    Task<Message?> CreateMessageAsync(CreateMessageRequest request, Snowflake authorId);
    Task<Message?> GetMessageAsync(Snowflake messageId);
    Task<List<Message>> GetChannelMessagesAsync(Snowflake channelId, int limit = 50, Snowflake? before = null, Snowflake? after = null);
    Task<Message?> EditMessageAsync(Snowflake messageId, string newContent, Snowflake userId);
    Task<bool> DeleteMessageAsync(Snowflake messageId, Snowflake userId);
    Task<bool> AddReactionAsync(Snowflake messageId, Snowflake userId, string emoji);
    Task<bool> RemoveReactionAsync(Snowflake messageId, Snowflake userId, string emoji);
}

public class MessageService(YurtCordDbContext context, IPermissionService permissionService, SnowflakeGenerator snowflakeGenerator) : IMessageService
{
    private readonly YurtCordDbContext _context = context;
    private readonly IPermissionService _permissionService = permissionService;
    private readonly SnowflakeGenerator _snowflakeGenerator = snowflakeGenerator;

    public async Task<Message?> CreateMessageAsync(CreateMessageRequest request, Snowflake authorId)
    {
        var channel = await _context.Channels.FindAsync(request.ChannelId);
        if (channel == null)
            return null;

        // Check permissions
        if (channel.GuildId != null)
        {
            var hasPermission = await _permissionService.HasChannelPermissionAsync(authorId, channel.Id, Permissions.SendMessages);
            if (!hasPermission)
                return null;
        }

        var message = new Message
        {
            Id = _snowflakeGenerator.Generate(),
            ChannelId = request.ChannelId,
            AuthorId = authorId,
            Content = request.Content,
            Timestamp = DateTime.UtcNow,
            Type = MessageType.Default,
            Tts = request.Tts,
            ReferencedMessageId = request.ReferencedMessageId
        };

        // Parse mentions
        var mentionedUsers = ParseMentions(request.Content);
        foreach (var userId in mentionedUsers)
        {
            message.Mentions.Add(new MessageMention
            {
                MessageId = message.Id,
                UserId = userId
            });
        }

        // Check for @everyone mention
        message.MentionEveryone = request.Content.Contains("@everyone") || request.Content.Contains("@here");

        _context.Messages.Add(message);

        // Update channel's last message timestamp
        channel.LastMessageAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetMessageAsync(message.Id);
    }

    public async Task<Message?> GetMessageAsync(Snowflake messageId)
    {
        return await _context.Messages
            .Include(m => m.Author)
            .Include(m => m.Mentions).ThenInclude(m => m.User)
            .Include(m => m.Attachments)
            .Include(m => m.Embeds).ThenInclude(e => e.Fields)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .Include(m => m.ReferencedMessage)
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }

    public async Task<List<Message>> GetChannelMessagesAsync(Snowflake channelId, int limit = 50, Snowflake? before = null, Snowflake? after = null)
    {
        var query = _context.Messages
            .Where(m => m.ChannelId == channelId)
            .Include(m => m.Author)
            .Include(m => m.Mentions).ThenInclude(m => m.User)
            .Include(m => m.Attachments)
            .Include(m => m.Embeds)
            .Include(m => m.Reactions)
            .OrderByDescending(m => m.Timestamp)
            .AsQueryable();

        if (before.HasValue)
            query = query.Where(m => m.Id < before.Value);

        if (after.HasValue)
            query = query.Where(m => m.Id > after.Value);

        return await query.Take(Math.Min(limit, 100)).ToListAsync();
    }

    public async Task<Message?> EditMessageAsync(Snowflake messageId, string newContent, Snowflake userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null || message.AuthorId != userId)
            return null;

        message.Content = newContent;
        message.EditedTimestamp = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetMessageAsync(messageId);
    }

    public async Task<bool> DeleteMessageAsync(Snowflake messageId, Snowflake userId)
    {
        var message = await _context.Messages
            .Include(m => m.Channel)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null)
            return false;

        // Check if user is the author or has ManageMessages permission
        bool canDelete = message.AuthorId == userId;

        if (!canDelete && message.Channel.GuildId != null)
        {
            canDelete = await _permissionService.HasChannelPermissionAsync(userId, message.ChannelId, Permissions.ManageMessages);
        }

        if (!canDelete)
            return false;

        _context.Messages.Remove(message);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AddReactionAsync(Snowflake messageId, Snowflake userId, string emoji)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null)
            return false;

        // Check if reaction already exists
        var existingReaction = await _context.Reactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.EmojiName == emoji);

        if (existingReaction != null)
            return false;

        var reaction = new Reaction
        {
            MessageId = messageId,
            UserId = userId,
            EmojiName = emoji,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reactions.Add(reaction);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveReactionAsync(Snowflake messageId, Snowflake userId, string emoji)
    {
        var reaction = await _context.Reactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId && r.EmojiName == emoji);

        if (reaction == null)
            return false;

        _context.Reactions.Remove(reaction);
        await _context.SaveChangesAsync();

        return true;
    }

    private List<Snowflake> ParseMentions(string content)
    {
        var mentions = new List<Snowflake>();
        var matches = System.Text.RegularExpressions.Regex.Matches(content, @"<@!?(\d+)>");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (long.TryParse(match.Groups[1].Value, out var id))
                mentions.Add(new Snowflake(id));
        }

        return mentions;
    }
}

public record CreateMessageRequest(
    Snowflake ChannelId,
    string Content,
    bool Tts = false,
    Snowflake? ReferencedMessageId = null
);
