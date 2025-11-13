using YurtCord.Core.Common;

namespace YurtCord.Core.Entities;

public class Emoji
{
    public Snowflake Id { get; set; }
    public Snowflake? GuildId { get; set; }
    public Guild? Guild { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Animated { get; set; }
    public bool Available { get; set; } = true;
    public bool RequireColons { get; set; } = true;
    public bool Managed { get; set; }
    public Snowflake? CreatorId { get; set; }
    public User? Creator { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<Role> RolesWhitelist { get; set; } = new();

    public string FormatForDiscord()
    {
        if (GuildId == null)
            return Name; // Unicode emoji

        return Animated ? $"<a:{Name}:{Id}>" : $"<:{Name}:{Id}>";
    }
}
