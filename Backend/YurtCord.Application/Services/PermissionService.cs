using Microsoft.EntityFrameworkCore;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;
using YurtCord.Infrastructure.Data;

namespace YurtCord.Application.Services;

public interface IPermissionService
{
    Task<Permissions> CalculateBasePermissionsAsync(Snowflake userId, Snowflake guildId);
    Task<Permissions> CalculateChannelPermissionsAsync(Snowflake userId, Snowflake channelId);
    Task<bool> HasPermissionAsync(Snowflake userId, Snowflake guildId, Permissions permission);
    Task<bool> HasChannelPermissionAsync(Snowflake userId, Snowflake channelId, Permissions permission);
}

public class PermissionService(YurtCordDbContext context) : IPermissionService
{
    private readonly YurtCordDbContext _context = context;

    public async Task<Permissions> CalculateBasePermissionsAsync(Snowflake userId, Snowflake guildId)
    {
        var guild = await _context.Guilds.FindAsync(guildId);
        if (guild == null)
            return Permissions.None;

        // Guild owner has all permissions
        if (guild.OwnerId == userId)
            return (Permissions)long.MaxValue;

        var member = await _context.GuildMembers
            .Include(gm => gm.Roles)
            .FirstOrDefaultAsync(gm => gm.GuildId == guildId && gm.UserId == userId);

        if (member == null)
            return Permissions.None;

        // Get @everyone role permissions
        var everyoneRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.GuildId == guildId && r.Name == "@everyone");

        var permissions = everyoneRole?.Permissions ?? Permissions.None;

        // Apply role permissions
        foreach (var role in member.Roles.OrderBy(r => r.Position))
        {
            permissions |= role.Permissions;

            // Administrator grants all permissions
            if (permissions.Has(Permissions.Administrator))
                return (Permissions)long.MaxValue;
        }

        return permissions;
    }

    public async Task<Permissions> CalculateChannelPermissionsAsync(Snowflake userId, Snowflake channelId)
    {
        var channel = await _context.Channels
            .Include(c => c.PermissionOverwrites)
            .Include(c => c.Guild)
            .FirstOrDefaultAsync(c => c.Id == channelId);

        if (channel == null || channel.GuildId == null)
            return Permissions.None;

        var guildId = channel.GuildId.Value;

        // Start with base guild permissions
        var permissions = await CalculateBasePermissionsAsync(userId, guildId);

        // Administrator bypasses channel overwrites
        if (permissions.Has(Permissions.Administrator))
            return permissions;

        var member = await _context.GuildMembers
            .Include(gm => gm.Roles)
            .FirstOrDefaultAsync(gm => gm.GuildId == guildId && gm.UserId == userId);

        if (member == null)
            return Permissions.None;

        // Apply @everyone overwrite
        var everyoneRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.GuildId == guildId && r.Name == "@everyone");

        if (everyoneRole != null)
        {
            var everyoneOverwrite = channel.PermissionOverwrites
                .FirstOrDefault(po => po.TargetId == everyoneRole.Id && po.Type == PermissionOverwriteType.Role);

            if (everyoneOverwrite != null)
            {
                permissions &= ~everyoneOverwrite.Deny;
                permissions |= everyoneOverwrite.Allow;
            }
        }

        // Apply role-specific overwrites
        var roleOverwrites = channel.PermissionOverwrites
            .Where(po => po.Type == PermissionOverwriteType.Role &&
                         member.Roles.Any(r => r.Id == po.TargetId))
            .OrderBy(po => member.Roles.First(r => r.Id == po.TargetId).Position);

        foreach (var overwrite in roleOverwrites)
        {
            permissions &= ~overwrite.Deny;
            permissions |= overwrite.Allow;
        }

        // Apply member-specific overwrite
        var memberOverwrite = channel.PermissionOverwrites
            .FirstOrDefault(po => po.TargetId == userId && po.Type == PermissionOverwriteType.Member);

        if (memberOverwrite != null)
        {
            permissions &= ~memberOverwrite.Deny;
            permissions |= memberOverwrite.Allow;
        }

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(Snowflake userId, Snowflake guildId, Permissions permission)
    {
        var permissions = await CalculateBasePermissionsAsync(userId, guildId);
        return permissions.Has(permission);
    }

    public async Task<bool> HasChannelPermissionAsync(Snowflake userId, Snowflake channelId, Permissions permission)
    {
        var permissions = await CalculateChannelPermissionsAsync(userId, channelId);
        return permissions.Has(permission);
    }
}
