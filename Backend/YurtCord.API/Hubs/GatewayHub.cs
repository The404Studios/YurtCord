using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using YurtCord.Application.Services;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;

namespace YurtCord.API.Hubs;

[Authorize]
public class GatewayHub(IAuthService authService, IMessageService messageService, IVoiceService voiceService) : Hub
{
    private static readonly ConcurrentDictionary<string, Snowflake> _connections = new();
    private static readonly ConcurrentDictionary<Snowflake, HashSet<string>> _userConnections = new();
    private static readonly ConcurrentDictionary<string, VoiceChannelState> _voiceChannels = new();

    private readonly IAuthService _authService = authService;
    private readonly IMessageService _messageService = messageService;
    private readonly IVoiceService _voiceService = voiceService;

    public override async Task OnConnectedAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            _connections[Context.ConnectionId] = user.Id;

            if (!_userConnections.TryGetValue(user.Id, out var connections))
            {
                connections = new HashSet<string>();
                _userConnections[user.Id] = connections;
            }

            connections.Add(Context.ConnectionId);

            await Clients.Caller.SendAsync("Ready", new
            {
                user = new
                {
                    id = user.Id.ToString(),
                    username = user.Username,
                    discriminator = user.Discriminator,
                    email = user.Email,
                    avatar = user.Avatar,
                    verified = user.Verified
                },
                sessionId = Context.ConnectionId
            });

            // Broadcast presence update
            await Clients.All.SendAsync("PresenceUpdate", new
            {
                userId = user.Id.ToString(),
                status = "online",
                updatedAt = DateTime.UtcNow
            });
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connections.TryRemove(Context.ConnectionId, out var userId))
        {
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);

                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);

                    // Broadcast offline status
                    await Clients.All.SendAsync("PresenceUpdate", new
                    {
                        userId = userId.ToString(),
                        status = "offline",
                        updatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string channelId, string content)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return;

        var message = await _messageService.CreateMessageAsync(
            new CreateMessageRequest(channelSnowflake, content),
            user.Id
        );

        if (message != null)
        {
            await Clients.All.SendAsync("MessageCreate", new
            {
                id = message.Id.ToString(),
                channelId = message.ChannelId.ToString(),
                content = message.Content,
                author = new
                {
                    id = message.Author.Id.ToString(),
                    username = message.Author.Username,
                    discriminator = message.Author.Discriminator,
                    avatar = message.Author.Avatar
                },
                timestamp = message.Timestamp,
                edited_timestamp = message.EditedTimestamp
            });
        }
    }

    public async Task TypingStart(string channelId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        await Clients.All.SendAsync("TypingStart", new
        {
            channelId,
            userId = user.Id.ToString(),
            timestamp = DateTime.UtcNow
        });
    }

    public async Task UpdatePresence(string status, string? customStatus = null)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        await Clients.All.SendAsync("PresenceUpdate", new
        {
            userId = user.Id.ToString(),
            status,
            customStatus,
            updatedAt = DateTime.UtcNow
        });
    }

    // ============================================
    // WebRTC Voice Channel Methods
    // ============================================

    public async Task JoinVoiceChannel(string channelId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return;

        // Join the voice service
        var connection = await _voiceService.JoinVoiceChannelAsync(user.Id, channelSnowflake);
        if (connection == null)
            return;

        // Add to SignalR group
        await Groups.AddToGroupAsync(Context.ConnectionId, $"voice_{channelId}");

        // Track in voice channel state
        var channelState = _voiceChannels.GetOrAdd(channelId, _ => new VoiceChannelState());
        channelState.AddUser(user.Id.ToString(), Context.ConnectionId);

        // Notify all users in the channel
        await Clients.Group($"voice_{channelId}").SendAsync("VoiceUserJoined", new
        {
            userId = user.Id.ToString(),
            username = user.Username,
            discriminator = user.Discriminator,
            avatar = user.Avatar,
            channelId,
            sessionId = connection.SessionId,
            connectionId = Context.ConnectionId,
            timestamp = DateTime.UtcNow
        });

        // Send current channel users to the new joiner
        var currentUsers = channelState.GetUsers();
        await Clients.Caller.SendAsync("VoiceChannelUsers", new
        {
            channelId,
            users = currentUsers
        });
    }

    public async Task LeaveVoiceChannel(string channelId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return;

        // Leave the voice service
        await _voiceService.LeaveVoiceChannelAsync(user.Id, channelSnowflake);

        // Remove from SignalR group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"voice_{channelId}");

        // Update voice channel state
        if (_voiceChannels.TryGetValue(channelId, out var channelState))
        {
            channelState.RemoveUser(user.Id.ToString());
        }

        // Notify all users in the channel
        await Clients.Group($"voice_{channelId}").SendAsync("VoiceUserLeft", new
        {
            userId = user.Id.ToString(),
            channelId,
            timestamp = DateTime.UtcNow
        });
    }

    // ============================================
    // WebRTC Signaling Methods
    // ============================================

    /// <summary>
    /// Send WebRTC offer to establish peer connection
    /// </summary>
    public async Task SendWebRTCOffer(string targetUserId, string channelId, object offer)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        // Find target user's connection
        if (_voiceChannels.TryGetValue(channelId, out var channelState))
        {
            var targetConnectionId = channelState.GetUserConnectionId(targetUserId);
            if (targetConnectionId != null)
            {
                await Clients.Client(targetConnectionId).SendAsync("WebRTCOffer", new
                {
                    fromUserId = user.Id.ToString(),
                    fromUsername = user.Username,
                    channelId,
                    offer
                });
            }
        }
    }

    /// <summary>
    /// Send WebRTC answer to complete peer connection
    /// </summary>
    public async Task SendWebRTCAnswer(string targetUserId, string channelId, object answer)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        // Find target user's connection
        if (_voiceChannels.TryGetValue(channelId, out var channelState))
        {
            var targetConnectionId = channelState.GetUserConnectionId(targetUserId);
            if (targetConnectionId != null)
            {
                await Clients.Client(targetConnectionId).SendAsync("WebRTCAnswer", new
                {
                    fromUserId = user.Id.ToString(),
                    channelId,
                    answer
                });
            }
        }
    }

    /// <summary>
    /// Send ICE candidate for WebRTC connection
    /// </summary>
    public async Task SendICECandidate(string targetUserId, string channelId, object candidate)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        // Find target user's connection
        if (_voiceChannels.TryGetValue(channelId, out var channelState))
        {
            var targetConnectionId = channelState.GetUserConnectionId(targetUserId);
            if (targetConnectionId != null)
            {
                await Clients.Client(targetConnectionId).SendAsync("ICECandidate", new
                {
                    fromUserId = user.Id.ToString(),
                    channelId,
                    candidate
                });
            }
        }
    }

    /// <summary>
    /// Update voice state (mute, deafen, speaking)
    /// </summary>
    public async Task UpdateVoiceState(string channelId, bool? mute, bool? deaf, bool? speaking)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        // Update in database if needed
        if (mute.HasValue || deaf.HasValue)
        {
            await _voiceService.UpdateVoiceStateAsync(user.Id, new VoiceStateUpdate
            {
                SelfMute = mute,
                SelfDeaf = deaf
            });
        }

        // Broadcast to channel
        await Clients.Group($"voice_{channelId}").SendAsync("VoiceStateUpdate", new
        {
            userId = user.Id.ToString(),
            channelId,
            mute,
            deaf,
            speaking,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Enable/disable video
    /// </summary>
    public async Task UpdateVideoState(string channelId, bool enabled)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        await _voiceService.UpdateVoiceStateAsync(user.Id, new VoiceStateUpdate
        {
            SelfVideo = enabled
        });

        await Clients.Group($"voice_{channelId}").SendAsync("VideoStateUpdate", new
        {
            userId = user.Id.ToString(),
            channelId,
            enabled,
            timestamp = DateTime.UtcNow
        });
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var token = Context.GetHttpContext()?.Request.Query["access_token"].ToString();
        if (string.IsNullOrEmpty(token))
            return null;

        return await _authService.GetUserFromTokenAsync(token);
    }

    public static async Task BroadcastGuildUpdate(IHubContext<GatewayHub> hubContext, object guildData)
    {
        await hubContext.Clients.All.SendAsync("GuildUpdate", guildData);
    }

    public static async Task BroadcastChannelUpdate(IHubContext<GatewayHub> hubContext, object channelData)
    {
        await hubContext.Clients.All.SendAsync("ChannelUpdate", channelData);
    }
}

/// <summary>
/// Tracks users in a voice channel for WebRTC signaling
/// </summary>
public class VoiceChannelState
{
    private readonly ConcurrentDictionary<string, string> _userConnections = new(); // userId -> connectionId

    public void AddUser(string userId, string connectionId)
    {
        _userConnections[userId] = connectionId;
    }

    public void RemoveUser(string userId)
    {
        _userConnections.TryRemove(userId, out _);
    }

    public string? GetUserConnectionId(string userId)
    {
        return _userConnections.TryGetValue(userId, out var connectionId) ? connectionId : null;
    }

    public List<object> GetUsers()
    {
        return _userConnections.Select(kvp => new
        {
            userId = kvp.Key,
            connectionId = kvp.Value
        } as object).ToList();
    }

    public int Count => _userConnections.Count;
}
