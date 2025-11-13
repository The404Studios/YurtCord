using YurtCord.Core.Common;

namespace YurtCord.Core.Entities;

public class Role
{
    public Snowflake Id { get; set; }
    public Snowflake GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public int Color { get; set; }
    public bool Hoist { get; set; }
    public string? Icon { get; set; }
    public string? UnicodeEmoji { get; set; }
    public int Position { get; set; }
    public Permissions Permissions { get; set; }
    public bool Managed { get; set; }
    public bool Mentionable { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<GuildMember> Members { get; set; } = new();
}

[Flags]
public enum Permissions : long
{
    None = 0,
    CreateInstantInvite = 1L << 0,
    KickMembers = 1L << 1,
    BanMembers = 1L << 2,
    Administrator = 1L << 3,
    ManageChannels = 1L << 4,
    ManageGuild = 1L << 5,
    AddReactions = 1L << 6,
    ViewAuditLog = 1L << 7,
    PrioritySpeaker = 1L << 8,
    Stream = 1L << 9,
    ViewChannel = 1L << 10,
    SendMessages = 1L << 11,
    SendTtsMessages = 1L << 12,
    ManageMessages = 1L << 13,
    EmbedLinks = 1L << 14,
    AttachFiles = 1L << 15,
    ReadMessageHistory = 1L << 16,
    MentionEveryone = 1L << 17,
    UseExternalEmojis = 1L << 18,
    ViewGuildInsights = 1L << 19,
    Connect = 1L << 20,
    Speak = 1L << 21,
    MuteMembers = 1L << 22,
    DeafenMembers = 1L << 23,
    MoveMembers = 1L << 24,
    UseVad = 1L << 25,
    ChangeNickname = 1L << 26,
    ManageNicknames = 1L << 27,
    ManageRoles = 1L << 28,
    ManageWebhooks = 1L << 29,
    ManageEmojisAndStickers = 1L << 30,
    UseApplicationCommands = 1L << 31,
    RequestToSpeak = 1L << 32,
    ManageEvents = 1L << 33,
    ManageThreads = 1L << 34,
    CreatePublicThreads = 1L << 35,
    CreatePrivateThreads = 1L << 36,
    UseExternalStickers = 1L << 37,
    SendMessagesInThreads = 1L << 38,
    UseEmbeddedActivities = 1L << 39,
    ModerateMembers = 1L << 40
}

public static class PermissionsExtensions
{
    public static bool Has(this Permissions permissions, Permissions permission)
    {
        // Administrator has all permissions
        if (permissions.HasFlag(Permissions.Administrator))
            return true;

        return permissions.HasFlag(permission);
    }

    public static Permissions Add(this Permissions permissions, Permissions permission)
    {
        return permissions | permission;
    }

    public static Permissions Remove(this Permissions permissions, Permissions permission)
    {
        return permissions & ~permission;
    }
}
