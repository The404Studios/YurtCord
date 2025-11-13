using YurtCord.Core.Common;

namespace YurtCord.Core.Entities;

public class Message
{
    public Snowflake Id { get; set; }
    public Snowflake ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;
    public Snowflake AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime? EditedTimestamp { get; set; }
    public bool Tts { get; set; }
    public bool MentionEveryone { get; set; }
    public MessageType Type { get; set; }
    public Snowflake? ReferencedMessageId { get; set; }
    public Message? ReferencedMessage { get; set; }
    public bool Pinned { get; set; }

    // Navigation properties
    public ICollection<MessageMention> Mentions { get; set; } = new();
    public ICollection<MessageRoleMention> MentionRoles { get; set; } = new();
    public ICollection<Attachment> Attachments { get; set; } = new();
    public ICollection<Embed> Embeds { get; set; } = new();
    public ICollection<Reaction> Reactions { get; set; } = new();
    public ICollection<MessageComponent> Components { get; set; } = new();
}

public enum MessageType
{
    Default = 0,
    RecipientAdd = 1,
    RecipientRemove = 2,
    Call = 3,
    ChannelNameChange = 4,
    ChannelIconChange = 5,
    ChannelPinnedMessage = 6,
    GuildMemberJoin = 7,
    UserPremiumGuildSubscription = 8,
    UserPremiumGuildSubscriptionTier1 = 9,
    UserPremiumGuildSubscriptionTier2 = 10,
    UserPremiumGuildSubscriptionTier3 = 11,
    ChannelFollowAdd = 12,
    GuildDiscoveryDisqualified = 14,
    GuildDiscoveryRequalified = 15,
    Reply = 19,
    ChatInputCommand = 20,
    ThreadStarterMessage = 21,
    GuildInviteReminder = 22,
    ContextMenuCommand = 23
}

public class MessageMention
{
    public int Id { get; set; }
    public Snowflake MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public Snowflake UserId { get; set; }
    public User User { get; set; } = null!;
}

public class MessageRoleMention
{
    public int Id { get; set; }
    public Snowflake MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public Snowflake RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class Attachment
{
    public Snowflake Id { get; set; }
    public Snowflake MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public string Filename { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ProxyUrl { get; set; } = string.Empty;
    public int? Height { get; set; }
    public int? Width { get; set; }
    public bool Ephemeral { get; set; }
}

public class Embed
{
    public int Id { get; set; }
    public Snowflake MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public string? Title { get; set; }
    public EmbedType Type { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public DateTime? Timestamp { get; set; }
    public int? Color { get; set; }
    public EmbedFooter? Footer { get; set; }
    public EmbedImage? Image { get; set; }
    public EmbedThumbnail? Thumbnail { get; set; }
    public EmbedVideo? Video { get; set; }
    public EmbedProvider? Provider { get; set; }
    public EmbedAuthor? Author { get; set; }
    public ICollection<EmbedField> Fields { get; set; } = new();
}

public enum EmbedType
{
    Rich,
    Image,
    Video,
    Gifv,
    Article,
    Link
}

public class EmbedFooter
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? ProxyIconUrl { get; set; }
}

public class EmbedImage
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ProxyUrl { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
}

public class EmbedThumbnail
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ProxyUrl { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
}

public class EmbedVideo
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ProxyUrl { get; set; }
    public int? Height { get; set; }
    public int? Width { get; set; }
}

public class EmbedProvider
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
}

public class EmbedAuthor
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? IconUrl { get; set; }
    public string? ProxyIconUrl { get; set; }
}

public class EmbedField
{
    public int Id { get; set; }
    public int EmbedId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Inline { get; set; }
}

public class Reaction
{
    public int Id { get; set; }
    public Snowflake MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public Snowflake UserId { get; set; }
    public User User { get; set; } = null!;
    public Snowflake? EmojiId { get; set; }
    public Emoji? Emoji { get; set; }
    public string? EmojiName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MessageComponent
{
    public int Id { get; set; }
    public Snowflake MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public ComponentType Type { get; set; }
    public string? CustomId { get; set; }
    public string? Label { get; set; }
    public ButtonStyle? Style { get; set; }
    public string? Url { get; set; }
    public bool? Disabled { get; set; }
    public string? Placeholder { get; set; }
    public int? MinValues { get; set; }
    public int? MaxValues { get; set; }
}

public enum ComponentType
{
    ActionRow = 1,
    Button = 2,
    SelectMenu = 3,
    TextInput = 4
}

public enum ButtonStyle
{
    Primary = 1,
    Secondary = 2,
    Success = 3,
    Danger = 4,
    Link = 5
}
