using Microsoft.EntityFrameworkCore;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;
using YurtCord.Infrastructure.Data;

namespace YurtCord.Application.Services;

public interface IVoiceService
{
    Task<VoiceConnection?> JoinVoiceChannelAsync(Snowflake userId, Snowflake channelId);
    Task<bool> LeaveVoiceChannelAsync(Snowflake userId, Snowflake channelId);
    Task<VoiceState?> UpdateVoiceStateAsync(Snowflake userId, VoiceStateUpdate update);
    Task<List<VoiceState>> GetChannelVoiceStatesAsync(Snowflake channelId);
    Task<VoiceConnection?> GetUserVoiceConnectionAsync(Snowflake userId);
    Task<string> GenerateVoiceTokenAsync(Snowflake userId, Snowflake channelId);
    Task<bool> ValidateVoiceTokenAsync(string token);
}

public class VoiceService(YurtCordDbContext context, IPermissionService permissionService) : IVoiceService
{
    private readonly YurtCordDbContext _context = context;
    private readonly IPermissionService _permissionService = permissionService;
    private static readonly Dictionary<Snowflake, VoiceConnection> _activeConnections = new();
    private static readonly object _lock = new();

    public async Task<VoiceConnection?> JoinVoiceChannelAsync(Snowflake userId, Snowflake channelId)
    {
        var channel = await _context.Channels
            .Include(c => c.Guild)
            .FirstOrDefaultAsync(c => c.Id == channelId);

        if (channel == null || !channel.IsVoice)
            return null;

        // Check permissions
        if (channel.GuildId != null)
        {
            var hasPermission = await _permissionService.HasChannelPermissionAsync(
                userId,
                channelId,
                Permissions.Connect
            );

            if (!hasPermission)
                return null;
        }

        // Check user limit
        if (channel.UserLimit.HasValue && channel.UserLimit.Value > 0)
        {
            var currentUsers = await _context.VoiceStates
                .CountAsync(vs => vs.ChannelId == channelId);

            if (currentUsers >= channel.UserLimit.Value)
                return null;
        }

        // Leave any existing voice channel
        await LeaveCurrentChannelAsync(userId);

        // Create or update voice state
        var voiceState = await _context.VoiceStates
            .FirstOrDefaultAsync(vs => vs.UserId == userId);

        if (voiceState == null)
        {
            voiceState = new VoiceState
            {
                UserId = userId,
                ChannelId = channelId,
                GuildId = channel.GuildId,
                SessionId = Guid.NewGuid().ToString(),
                Deaf = false,
                Mute = false,
                SelfDeaf = false,
                SelfMute = false,
                SelfVideo = false,
                Suppress = false
            };
            _context.VoiceStates.Add(voiceState);
        }
        else
        {
            voiceState.ChannelId = channelId;
            voiceState.GuildId = channel.GuildId;
            voiceState.SessionId = Guid.NewGuid().ToString();
        }

        await _context.SaveChangesAsync();

        // Create voice connection
        var connection = new VoiceConnection
        {
            UserId = userId,
            ChannelId = channelId,
            SessionId = voiceState.SessionId,
            ConnectedAt = DateTime.UtcNow,
            Token = await GenerateVoiceTokenAsync(userId, channelId)
        };

        lock (_lock)
        {
            _activeConnections[userId] = connection;
        }

        return connection;
    }

    public async Task<bool> LeaveVoiceChannelAsync(Snowflake userId, Snowflake channelId)
    {
        return await LeaveCurrentChannelAsync(userId);
    }

    private async Task<bool> LeaveCurrentChannelAsync(Snowflake userId)
    {
        var voiceState = await _context.VoiceStates
            .FirstOrDefaultAsync(vs => vs.UserId == userId);

        if (voiceState != null)
        {
            _context.VoiceStates.Remove(voiceState);
            await _context.SaveChangesAsync();
        }

        lock (_lock)
        {
            _activeConnections.Remove(userId);
        }

        return true;
    }

    public async Task<VoiceState?> UpdateVoiceStateAsync(Snowflake userId, VoiceStateUpdate update)
    {
        var voiceState = await _context.VoiceStates
            .FirstOrDefaultAsync(vs => vs.UserId == userId);

        if (voiceState == null)
            return null;

        if (update.SelfMute.HasValue)
            voiceState.SelfMute = update.SelfMute.Value;

        if (update.SelfDeaf.HasValue)
            voiceState.SelfDeaf = update.SelfDeaf.Value;

        if (update.SelfVideo.HasValue)
            voiceState.SelfVideo = update.SelfVideo.Value;

        await _context.SaveChangesAsync();

        return voiceState;
    }

    public async Task<List<VoiceState>> GetChannelVoiceStatesAsync(Snowflake channelId)
    {
        return await _context.VoiceStates
            .Include(vs => vs.User)
            .Where(vs => vs.ChannelId == channelId)
            .ToListAsync();
    }

    public Task<VoiceConnection?> GetUserVoiceConnectionAsync(Snowflake userId)
    {
        lock (_lock)
        {
            return Task.FromResult(
                _activeConnections.TryGetValue(userId, out var connection)
                    ? connection
                    : null
            );
        }
    }

    public Task<string> GenerateVoiceTokenAsync(Snowflake userId, Snowflake channelId)
    {
        // Generate a secure token for voice connection
        var tokenData = $"{userId}:{channelId}:{DateTime.UtcNow.Ticks}";
        var token = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(tokenData)
        );
        return Task.FromResult(token);
    }

    public Task<bool> ValidateVoiceTokenAsync(string token)
    {
        try
        {
            var decoded = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(token)
            );
            var parts = decoded.Split(':');

            if (parts.Length != 3)
                return Task.FromResult(false);

            // Check if token is not too old (10 minutes)
            if (long.TryParse(parts[2], out var ticks))
            {
                var timestamp = new DateTime(ticks);
                var age = DateTime.UtcNow - timestamp;
                return Task.FromResult(age.TotalMinutes <= 10);
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}

public class VoiceConnection
{
    public Snowflake UserId { get; set; }
    public Snowflake ChannelId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public string? IceServers { get; set; }
}

public class VoiceStateUpdate
{
    public bool? SelfMute { get; set; }
    public bool? SelfDeaf { get; set; }
    public bool? SelfVideo { get; set; }
}
