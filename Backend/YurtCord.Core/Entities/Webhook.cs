using YurtCord.Core.Common;

namespace YurtCord.Core.Entities;

public class Webhook
{
    public Snowflake Id { get; set; }
    public WebhookType Type { get; set; }
    public Snowflake? GuildId { get; set; }
    public Guild? Guild { get; set; }
    public Snowflake ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;
    public Snowflake? CreatorId { get; set; }
    public User? Creator { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }
    public string Token { get; set; } = string.Empty;
    public Snowflake? ApplicationId { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum WebhookType
{
    Incoming = 1,
    ChannelFollower = 2,
    Application = 3
}
