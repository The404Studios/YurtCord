using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YurtCord.Application.Services;
using YurtCord.Core.Common;

namespace YurtCord.API.Controllers;

[ApiController]
[Route("api/voice")]
[Authorize]
public class VoiceController(IVoiceService voiceService, IAuthService authService) : ControllerBase
{
    private readonly IVoiceService _voiceService = voiceService;
    private readonly IAuthService _authService = authService;

    private async Task<Snowflake?> GetCurrentUserIdAsync()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var user = await _authService.GetUserFromTokenAsync(token);
        return user?.Id;
    }

    /// <summary>
    /// Get available voice regions (STUN/TURN servers)
    /// </summary>
    [HttpGet("regions")]
    public IActionResult GetVoiceRegions()
    {
        var regions = new object[]
        {
            new
            {
                id = "us-west",
                name = "US West",
                optimal = true,
                deprecated = false,
                custom = false
            },
            new
            {
                id = "us-east",
                name = "US East",
                optimal = false,
                deprecated = false,
                custom = false
            },
            new
            {
                id = "europe",
                name = "Europe",
                optimal = false,
                deprecated = false,
                custom = false
            }
        };

        return Ok(regions);
    }

    /// <summary>
    /// Join a voice channel
    /// </summary>
    [HttpPost("channels/{channelId}/join")]
    public async Task<IActionResult> JoinVoiceChannel(string channelId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        var connection = await _voiceService.JoinVoiceChannelAsync(userId.Value, channelSnowflake);
        if (connection == null)
            return Forbid();

        // Get ICE servers configuration
        var iceServers = GetIceServersConfiguration();

        return Ok(new
        {
            token = connection.Token,
            sessionId = connection.SessionId,
            channelId = connection.ChannelId.ToString(),
            userId = connection.UserId.ToString(),
            iceServers = iceServers
        });
    }

    /// <summary>
    /// Leave a voice channel
    /// </summary>
    [HttpPost("channels/{channelId}/leave")]
    public async Task<IActionResult> LeaveVoiceChannel(string channelId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        var success = await _voiceService.LeaveVoiceChannelAsync(userId.Value, channelSnowflake);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Update voice state (mute, deafen, video)
    /// </summary>
    [HttpPatch("state")]
    public async Task<IActionResult> UpdateVoiceState([FromBody] VoiceStateUpdate update)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
            return Unauthorized();

        var voiceState = await _voiceService.UpdateVoiceStateAsync(userId.Value, update);
        if (voiceState == null)
            return NotFound();

        return Ok(new
        {
            userId = voiceState.UserId.ToString(),
            channelId = voiceState.ChannelId.ToString(),
            selfMute = voiceState.SelfMute,
            selfDeaf = voiceState.SelfDeaf,
            selfVideo = voiceState.SelfVideo,
            mute = voiceState.Mute,
            deaf = voiceState.Deaf
        });
    }

    /// <summary>
    /// Get users in a voice channel
    /// </summary>
    [HttpGet("channels/{channelId}/users")]
    public async Task<IActionResult> GetChannelUsers(string channelId)
    {
        if (!Snowflake.TryParse(channelId, out var channelSnowflake))
            return BadRequest(new { error = "Invalid channel ID" });

        var voiceStates = await _voiceService.GetChannelVoiceStatesAsync(channelSnowflake);

        return Ok(voiceStates.Select(vs => new
        {
            userId = vs.UserId.ToString(),
            username = vs.User.Username,
            discriminator = vs.User.Discriminator,
            avatar = vs.User.Avatar,
            sessionId = vs.SessionId,
            selfMute = vs.SelfMute,
            selfDeaf = vs.SelfDeaf,
            selfVideo = vs.SelfVideo,
            mute = vs.Mute,
            deaf = vs.Deaf,
            suppress = vs.Suppress
        }));
    }

    /// <summary>
    /// Get ICE servers configuration for WebRTC
    /// </summary>
    private object GetIceServersConfiguration()
    {
        return new
        {
            iceServers = new[]
            {
                // Public STUN servers
                new
                {
                    urls = new[] { "stun:stun.l.google.com:19302" }
                },
                new
                {
                    urls = new[] { "stun:stun1.l.google.com:19302" }
                },
                new
                {
                    urls = new[] { "stun:stun2.l.google.com:19302" }
                },
                // TURN server (you would configure your own in production)
                new
                {
                    urls = new[] { "turn:your-turn-server.com:3478" },
                    username = "yurtcord",
                    credential = "your-turn-credential"
                }
            },
            iceTransportPolicy = "all",
            bundlePolicy = "max-bundle",
            rtcpMuxPolicy = "require"
        };
    }
}
