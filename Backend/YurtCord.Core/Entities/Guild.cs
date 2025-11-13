using YurtCord.Core.Common;

namespace YurtCord.Core.Entities;

public class Guild
{
    public Snowflake Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Banner { get; set; }
    public string? Splash { get; set; }
    public Snowflake OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public VerificationLevel VerificationLevel { get; set; }
    public DefaultMessageNotificationLevel DefaultMessageNotifications { get; set; }
    public ExplicitContentFilterLevel ExplicitContentFilter { get; set; }
    public int AfkTimeout { get; set; }
    public Snowflake? AfkChannelId { get; set; }
    public Snowflake? SystemChannelId { get; set; }
    public bool PremiumProgressBarEnabled { get; set; }
    public GuildFeatures Features { get; set; }
    public int MaxMembers { get; set; } = 500000;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Channel> Channels { get; set; } = new();
    public ICollection<Role> Roles { get; set; } = new();
    public ICollection<GuildMember> Members { get; set; } = new();
    public ICollection<Emoji> Emojis { get; set; } = new();
    public ICollection<GuildBan> Bans { get; set; } = new();
    public ICollection<Invite> Invites { get; set; } = new();
    public ICollection<Webhook> Webhooks { get; set; } = new();
}

public enum VerificationLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4
}

public enum DefaultMessageNotificationLevel
{
    AllMessages = 0,
    OnlyMentions = 1
}

public enum ExplicitContentFilterLevel
{
    Disabled = 0,
    MembersWithoutRoles = 1,
    AllMembers = 2
}

[Flags]
public enum GuildFeatures
{
    None = 0,
    InviteSplash = 1 << 0,
    VipRegions = 1 << 1,
    VanityUrl = 1 << 2,
    Verified = 1 << 3,
    Partnered = 1 << 4,
    Community = 1 << 5,
    Commerce = 1 << 6,
    News = 1 << 7,
    Discoverable = 1 << 8,
    Featurable = 1 << 9,
    AnimatedIcon = 1 << 10,
    Banner = 1 << 11,
    WelcomeScreenEnabled = 1 << 12,
    MemberVerificationGateEnabled = 1 << 13,
    PreviewEnabled = 1 << 14,
    TicketedEventsEnabled = 1 << 15,
    MonetizationEnabled = 1 << 16,
    MoreStickers = 1 << 17,
    PrivateThreads = 1 << 18,
    RoleIcons = 1 << 19
}

public class GuildMember
{
    public int Id { get; set; }
    public Snowflake GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Snowflake UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Nickname { get; set; }
    public string? Avatar { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? PremiumSince { get; set; }
    public bool Deaf { get; set; }
    public bool Mute { get; set; }
    public bool Pending { get; set; }
    public DateTime? CommunicationDisabledUntil { get; set; }

    // Navigation properties
    public ICollection<Role> Roles { get; set; } = new();

    public bool IsMuted => CommunicationDisabledUntil.HasValue && CommunicationDisabledUntil.Value > DateTime.UtcNow;
}

public class GuildBan
{
    public int Id { get; set; }
    public Snowflake GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Snowflake UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Reason { get; set; }
    public Snowflake BannedBy { get; set; }
    public DateTime BannedAt { get; set; }
}

public class Invite
{
    public string Code { get; set; } = string.Empty;
    public Snowflake GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Snowflake ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;
    public Snowflake InviterId { get; set; }
    public User Inviter { get; set; } = null!;
    public int MaxUses { get; set; }
    public int Uses { get; set; }
    public int MaxAge { get; set; }
    public bool Temporary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    public bool IsMaxedOut => MaxUses > 0 && Uses >= MaxUses;
}
