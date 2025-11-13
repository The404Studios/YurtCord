using YurtCord.Core.Common;

namespace YurtCord.Core.Entities;

public class Channel
{
    public Snowflake Id { get; set; }
    public ChannelType Type { get; set; }
    public Snowflake? GuildId { get; set; }
    public Guild? Guild { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public int Position { get; set; }
    public bool Nsfw { get; set; }
    public Snowflake? ParentId { get; set; }
    public Channel? Parent { get; set; }
    public int? RateLimitPerUser { get; set; }
    public int? Bitrate { get; set; }
    public int? UserLimit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }

    // Navigation properties
    public ICollection<Message> Messages { get; set; } = new();
    public ICollection<PermissionOverwrite> PermissionOverwrites { get; set; } = new();
    public ICollection<Channel> Children { get; set; } = new();
    public ICollection<Webhook> Webhooks { get; set; } = new();
    public ICollection<VoiceState> VoiceStates { get; set; } = new();
    public ICollection<ChannelPin> Pins { get; set; } = new();

    public bool IsThread => Type is ChannelType.GuildNewsThread or ChannelType.GuildPublicThread or ChannelType.GuildPrivateThread;
    public bool IsVoice => Type is ChannelType.GuildVoice or ChannelType.GuildStageVoice;
    public bool IsText => Type is ChannelType.GuildText or ChannelType.DM or ChannelType.GroupDM;
}

public enum ChannelType
{
    GuildText = 0,
    DM = 1,
    GuildVoice = 2,
    GroupDM = 3,
    GuildCategory = 4,
    GuildNews = 5,
    GuildStore = 6,
    GuildNewsThread = 10,
    GuildPublicThread = 11,
    GuildPrivateThread = 12,
    GuildStageVoice = 13,
    GuildDirectory = 14,
    GuildForum = 15
}

public class PermissionOverwrite
{
    public int Id { get; set; }
    public Snowflake ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;
    public Snowflake TargetId { get; set; }
    public PermissionOverwriteType Type { get; set; }
    public Permissions Allow { get; set; }
    public Permissions Deny { get; set; }
}

public enum PermissionOverwriteType
{
    Role = 0,
    Member = 1
}

public class ChannelPin
{
    public int Id { get; set; }
    public Snowflake ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;
    public Snowflake MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public Snowflake PinnedBy { get; set; }
    public DateTime PinnedAt { get; set; }
}

public class VoiceState
{
    public int Id { get; set; }
    public Snowflake UserId { get; set; }
    public User User { get; set; } = null!;
    public Snowflake? GuildId { get; set; }
    public Snowflake ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;
    public string SessionId { get; set; } = string.Empty;
    public bool Deaf { get; set; }
    public bool Mute { get; set; }
    public bool SelfDeaf { get; set; }
    public bool SelfMute { get; set; }
    public bool SelfVideo { get; set; }
    public bool Suppress { get; set; }
    public DateTime? RequestToSpeakTimestamp { get; set; }
}
