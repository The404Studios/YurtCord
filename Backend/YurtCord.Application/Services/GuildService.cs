using Microsoft.EntityFrameworkCore;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;
using YurtCord.Infrastructure.Data;

namespace YurtCord.Application.Services;

public interface IGuildService
{
    Task<Guild?> CreateGuildAsync(CreateGuildRequest request, Snowflake ownerId);
    Task<Guild?> GetGuildAsync(Snowflake guildId);
    Task<List<Guild>> GetUserGuildsAsync(Snowflake userId);
    Task<Guild?> UpdateGuildAsync(Snowflake guildId, UpdateGuildRequest request, Snowflake userId);
    Task<bool> DeleteGuildAsync(Snowflake guildId, Snowflake userId);
    Task<GuildMember?> AddMemberAsync(Snowflake guildId, Snowflake userId);
    Task<bool> RemoveMemberAsync(Snowflake guildId, Snowflake memberId, Snowflake removedBy);
    Task<Channel?> CreateChannelAsync(Snowflake guildId, CreateChannelRequest request, Snowflake userId);
    Task<Role?> CreateRoleAsync(Snowflake guildId, CreateRoleRequest request, Snowflake userId);
}

public class GuildService : IGuildService
{
    private readonly YurtCordDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly SnowflakeGenerator _snowflakeGenerator;

    public GuildService(YurtCordDbContext context, IPermissionService permissionService, SnowflakeGenerator snowflakeGenerator)
    {
        _context = context;
        _permissionService = permissionService;
        _snowflakeGenerator = snowflakeGenerator;
    }

    public async Task<Guild?> CreateGuildAsync(CreateGuildRequest request, Snowflake ownerId)
    {
        var guild = new Guild
        {
            Id = _snowflakeGenerator.Generate(),
            Name = request.Name,
            Description = request.Description,
            OwnerId = ownerId,
            VerificationLevel = VerificationLevel.None,
            DefaultMessageNotifications = DefaultMessageNotificationLevel.AllMessages,
            ExplicitContentFilter = ExplicitContentFilterLevel.Disabled,
            AfkTimeout = 300,
            Features = GuildFeatures.None,
            CreatedAt = DateTime.UtcNow
        };

        _context.Guilds.Add(guild);

        // Create @everyone role
        var everyoneRole = new Role
        {
            Id = guild.Id, // @everyone role has the same ID as guild
            GuildId = guild.Id,
            Name = "@everyone",
            Color = 0,
            Hoist = false,
            Position = 0,
            Permissions = Permissions.ViewChannel | Permissions.SendMessages | Permissions.ReadMessageHistory,
            Managed = false,
            Mentionable = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Roles.Add(everyoneRole);

        // Add owner as member
        var ownerMember = new GuildMember
        {
            GuildId = guild.Id,
            UserId = ownerId,
            JoinedAt = DateTime.UtcNow,
            Deaf = false,
            Mute = false,
            Pending = false
        };

        _context.GuildMembers.Add(ownerMember);

        // Create default text channel
        var generalChannel = new Channel
        {
            Id = _snowflakeGenerator.Generate(),
            Type = ChannelType.GuildText,
            GuildId = guild.Id,
            Name = "general",
            Topic = "General discussion",
            Position = 0,
            Nsfw = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Channels.Add(generalChannel);

        // Create default voice channel
        var voiceChannel = new Channel
        {
            Id = _snowflakeGenerator.Generate(),
            Type = ChannelType.GuildVoice,
            GuildId = guild.Id,
            Name = "General Voice",
            Position = 1,
            Bitrate = 64000,
            UserLimit = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Channels.Add(voiceChannel);

        await _context.SaveChangesAsync();

        return await GetGuildAsync(guild.Id);
    }

    public async Task<Guild?> GetGuildAsync(Snowflake guildId)
    {
        return await _context.Guilds
            .Include(g => g.Owner)
            .Include(g => g.Channels.OrderBy(c => c.Position))
            .Include(g => g.Roles.OrderBy(r => r.Position))
            .Include(g => g.Emojis)
            .FirstOrDefaultAsync(g => g.Id == guildId);
    }

    public async Task<List<Guild>> GetUserGuildsAsync(Snowflake userId)
    {
        return await _context.GuildMembers
            .Where(gm => gm.UserId == userId)
            .Include(gm => gm.Guild)
            .Select(gm => gm.Guild)
            .ToListAsync();
    }

    public async Task<Guild?> UpdateGuildAsync(Snowflake guildId, UpdateGuildRequest request, Snowflake userId)
    {
        var guild = await _context.Guilds.FindAsync(guildId);
        if (guild == null)
            return null;

        // Check permissions
        var hasPermission = await _permissionService.HasPermissionAsync(userId, guildId, Permissions.ManageGuild);
        if (!hasPermission && guild.OwnerId != userId)
            return null;

        if (request.Name != null)
            guild.Name = request.Name;

        if (request.Description != null)
            guild.Description = request.Description;

        if (request.Icon != null)
            guild.Icon = request.Icon;

        if (request.Banner != null)
            guild.Banner = request.Banner;

        await _context.SaveChangesAsync();

        return await GetGuildAsync(guildId);
    }

    public async Task<bool> DeleteGuildAsync(Snowflake guildId, Snowflake userId)
    {
        var guild = await _context.Guilds.FindAsync(guildId);
        if (guild == null || guild.OwnerId != userId)
            return false;

        _context.Guilds.Remove(guild);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<GuildMember?> AddMemberAsync(Snowflake guildId, Snowflake userId)
    {
        // Check if already a member
        var existingMember = await _context.GuildMembers
            .FirstOrDefaultAsync(gm => gm.GuildId == guildId && gm.UserId == userId);

        if (existingMember != null)
            return existingMember;

        var member = new GuildMember
        {
            GuildId = guildId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            Deaf = false,
            Mute = false,
            Pending = false
        };

        _context.GuildMembers.Add(member);
        await _context.SaveChangesAsync();

        return member;
    }

    public async Task<bool> RemoveMemberAsync(Snowflake guildId, Snowflake memberId, Snowflake removedBy)
    {
        var guild = await _context.Guilds.FindAsync(guildId);
        if (guild == null)
            return false;

        // Can't remove the owner
        if (guild.OwnerId == memberId)
            return false;

        var member = await _context.GuildMembers
            .FirstOrDefaultAsync(gm => gm.GuildId == guildId && gm.UserId == memberId);

        if (member == null)
            return false;

        // Check permissions
        var hasPermission = await _permissionService.HasPermissionAsync(removedBy, guildId, Permissions.KickMembers);
        if (!hasPermission && guild.OwnerId != removedBy)
            return false;

        _context.GuildMembers.Remove(member);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Channel?> CreateChannelAsync(Snowflake guildId, CreateChannelRequest request, Snowflake userId)
    {
        // Check permissions
        var hasPermission = await _permissionService.HasPermissionAsync(userId, guildId, Permissions.ManageChannels);
        if (!hasPermission)
            return null;

        var channel = new Channel
        {
            Id = _snowflakeGenerator.Generate(),
            Type = request.Type,
            GuildId = guildId,
            Name = request.Name,
            Topic = request.Topic,
            Position = request.Position,
            Nsfw = request.Nsfw,
            ParentId = request.ParentId,
            RateLimitPerUser = request.RateLimitPerUser,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Type == ChannelType.GuildVoice || request.Type == ChannelType.GuildStageVoice)
        {
            channel.Bitrate = request.Bitrate ?? 64000;
            channel.UserLimit = request.UserLimit ?? 0;
        }

        _context.Channels.Add(channel);
        await _context.SaveChangesAsync();

        return channel;
    }

    public async Task<Role?> CreateRoleAsync(Snowflake guildId, CreateRoleRequest request, Snowflake userId)
    {
        // Check permissions
        var hasPermission = await _permissionService.HasPermissionAsync(userId, guildId, Permissions.ManageRoles);
        if (!hasPermission)
            return null;

        var role = new Role
        {
            Id = _snowflakeGenerator.Generate(),
            GuildId = guildId,
            Name = request.Name,
            Color = request.Color,
            Hoist = request.Hoist,
            Position = request.Position,
            Permissions = request.Permissions,
            Managed = false,
            Mentionable = request.Mentionable,
            CreatedAt = DateTime.UtcNow
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return role;
    }
}

public record CreateGuildRequest(string Name, string? Description = null);
public record UpdateGuildRequest(string? Name = null, string? Description = null, string? Icon = null, string? Banner = null);
public record CreateChannelRequest(ChannelType Type, string Name, string? Topic = null, int Position = 0, bool Nsfw = false, Snowflake? ParentId = null, int? RateLimitPerUser = null, int? Bitrate = null, int? UserLimit = null);
public record CreateRoleRequest(string Name, int Color = 0, bool Hoist = false, int Position = 0, Permissions Permissions = Permissions.None, bool Mentionable = false);
