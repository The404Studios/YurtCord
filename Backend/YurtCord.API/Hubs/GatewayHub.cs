using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using YurtCord.Application.Services;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;

namespace YurtCord.API.Hubs;

[Authorize]
public class GatewayHub : Hub
{
    private static readonly ConcurrentDictionary<string, Snowflake> _connections = new();
    private static readonly ConcurrentDictionary<Snowflake, HashSet<string>> _userConnections = new();

    private readonly IAuthService _authService;
    private readonly IMessageService _messageService;

    public GatewayHub(IAuthService authService, IMessageService messageService)
    {
        _authService = authService;
        _messageService = messageService;
    }

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

    public async Task JoinVoiceChannel(string channelId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"voice_{channelId}");

        await Clients.Group($"voice_{channelId}").SendAsync("VoiceStateUpdate", new
        {
            userId = user.Id.ToString(),
            channelId,
            joined = true,
            timestamp = DateTime.UtcNow
        });
    }

    public async Task LeaveVoiceChannel(string channelId)
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"voice_{channelId}");

        await Clients.Group($"voice_{channelId}").SendAsync("VoiceStateUpdate", new
        {
            userId = user.Id.ToString(),
            channelId,
            joined = false,
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
