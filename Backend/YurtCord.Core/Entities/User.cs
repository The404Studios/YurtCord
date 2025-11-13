using YurtCord.Core.Common;

namespace YurtCord.Core.Entities;

public class User
{
    public Snowflake Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Discriminator { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Banner { get; set; }
    public string? Bio { get; set; }
    public bool Verified { get; set; }
    public bool MfaEnabled { get; set; }
    public UserFlags Flags { get; set; }
    public PremiumType PremiumType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public UserPresence? Presence { get; set; }
    public ICollection<GuildMember> GuildMemberships { get; set; } = new();
    public ICollection<Message> Messages { get; set; } = new();
    public ICollection<Relationship> Relationships { get; set; } = new();
    public ICollection<Relationship> RelatedBy { get; set; } = new();

    public string Tag => $"{Username}#{Discriminator}";
}

[Flags]
public enum UserFlags
{
    None = 0,
    Staff = 1 << 0,
    Partner = 1 << 1,
    HypeSquad = 1 << 2,
    BugHunter = 1 << 3,
    HypeSquadBravery = 1 << 6,
    HypeSquadBrilliance = 1 << 7,
    HypeSquadBalance = 1 << 8,
    EarlySupporter = 1 << 9,
    BugHunterLevel2 = 1 << 14,
    VerifiedBot = 1 << 16,
    VerifiedBotDeveloper = 1 << 17,
    CertifiedModerator = 1 << 18,
    BotHttpInteractions = 1 << 19
}

public enum PremiumType
{
    None = 0,
    NitroClassic = 1,
    Nitro = 2,
    NitroBasic = 3
}

public class UserPresence
{
    public Snowflake UserId { get; set; }
    public User User { get; set; } = null!;
    public PresenceStatus Status { get; set; }
    public string? CustomStatus { get; set; }
    public ICollection<Activity> Activities { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public enum PresenceStatus
{
    Online,
    Idle,
    DoNotDisturb,
    Invisible,
    Offline
}

public class Activity
{
    public int Id { get; set; }
    public Snowflake UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ActivityType Type { get; set; }
    public string? Details { get; set; }
    public string? State { get; set; }
    public string? LargeImage { get; set; }
    public string? SmallImage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndsAt { get; set; }
}

public enum ActivityType
{
    Playing = 0,
    Streaming = 1,
    Listening = 2,
    Watching = 3,
    Custom = 4,
    Competing = 5
}

public class Relationship
{
    public int Id { get; set; }
    public Snowflake UserId { get; set; }
    public User User { get; set; } = null!;
    public Snowflake TargetUserId { get; set; }
    public User TargetUser { get; set; } = null!;
    public RelationshipType Type { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum RelationshipType
{
    Friend = 1,
    Blocked = 2,
    IncomingRequest = 3,
    OutgoingRequest = 4
}
