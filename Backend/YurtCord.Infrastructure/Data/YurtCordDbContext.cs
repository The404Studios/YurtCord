using Microsoft.EntityFrameworkCore;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;

namespace YurtCord.Infrastructure.Data;

public class YurtCordDbContext : DbContext
{
    private readonly SnowflakeGenerator _snowflakeGenerator;

    public YurtCordDbContext(DbContextOptions<YurtCordDbContext> options, SnowflakeGenerator snowflakeGenerator)
        : base(options)
    {
        _snowflakeGenerator = snowflakeGenerator;
    }

    // Users
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPresence> UserPresences => Set<UserPresence>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Relationship> Relationships => Set<Relationship>();

    // Guilds
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildMember> GuildMembers => Set<GuildMember>();
    public DbSet<GuildBan> GuildBans => Set<GuildBan>();
    public DbSet<Invite> Invites => Set<Invite>();

    // Channels
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<PermissionOverwrite> PermissionOverwrites => Set<PermissionOverwrite>();
    public DbSet<ChannelPin> ChannelPins => Set<ChannelPin>();
    public DbSet<VoiceState> VoiceStates => Set<VoiceState>();

    // Messages
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageMention> MessageMentions => Set<MessageMention>();
    public DbSet<MessageRoleMention> MessageRoleMentions => Set<MessageRoleMention>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Embed> Embeds => Set<Embed>();
    public DbSet<EmbedField> EmbedFields => Set<EmbedField>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<MessageComponent> MessageComponents => Set<MessageComponent>();

    // Roles & Permissions
    public DbSet<Role> Roles => Set<Role>();

    // Emojis
    public DbSet<Emoji> Emojis => Set<Emoji>();

    // Webhooks
    public DbSet<Webhook> Webhooks => Set<Webhook>();

    // Audit Logs
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Snowflake value conversions
        modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        modelBuilder.Entity<Guild>()
            .Property(g => g.Id)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        modelBuilder.Entity<Channel>()
            .Property(c => c.Id)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        modelBuilder.Entity<Message>()
            .Property(m => m.Id)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        modelBuilder.Entity<Role>()
            .Property(r => r.Id)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        modelBuilder.Entity<Emoji>()
            .Property(e => e.Id)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        modelBuilder.Entity<Webhook>()
            .Property(w => w.Id)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        modelBuilder.Entity<Attachment>()
            .Property(a => a.Id)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        modelBuilder.Entity<AuditLogEntry>()
            .Property(a => a.Id)
            .HasConversion(
                v => v.Value,
                v => new Snowflake(v));

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => new { u.Username, u.Discriminator }).IsUnique();

            entity.HasOne(u => u.Presence)
                .WithOne(p => p.User)
                .HasForeignKey<UserPresence>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPresence Configuration
        modelBuilder.Entity<UserPresence>(entity =>
        {
            entity.HasKey(p => p.UserId);

            entity.Property(p => p.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // Activity Configuration
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // Relationship Configuration
        modelBuilder.Entity<Relationship>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.UserId, r.TargetUserId }).IsUnique();

            entity.Property(r => r.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(r => r.TargetUserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.HasOne(r => r.User)
                .WithMany(u => u.Relationships)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.TargetUser)
                .WithMany(u => u.RelatedBy)
                .HasForeignKey(r => r.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Guild Configuration
        modelBuilder.Entity<Guild>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.HasIndex(g => g.Name);

            entity.Property(g => g.OwnerId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(g => g.AfkChannelId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.Property(g => g.SystemChannelId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.HasOne(g => g.Owner)
                .WithMany()
                .HasForeignKey(g => g.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // GuildMember Configuration
        modelBuilder.Entity<GuildMember>(entity =>
        {
            entity.HasKey(gm => gm.Id);
            entity.HasIndex(gm => new { gm.GuildId, gm.UserId }).IsUnique();

            entity.Property(gm => gm.GuildId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(gm => gm.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.HasOne(gm => gm.Guild)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(gm => gm.User)
                .WithMany(u => u.GuildMemberships)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(gm => gm.Roles)
                .WithMany(r => r.Members)
                .UsingEntity(j => j.ToTable("GuildMemberRoles"));
        });

        // GuildBan Configuration
        modelBuilder.Entity<GuildBan>(entity =>
        {
            entity.HasKey(gb => gb.Id);
            entity.HasIndex(gb => new { gb.GuildId, gb.UserId }).IsUnique();

            entity.Property(gb => gb.GuildId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(gb => gb.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(gb => gb.BannedBy)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // Channel Configuration
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.GuildId, c.Position });

            entity.Property(c => c.GuildId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.Property(c => c.ParentId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.HasOne(c => c.Guild)
                .WithMany(g => g.Channels)
                .HasForeignKey(c => c.GuildId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Message Configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.ChannelId, m.Timestamp });
            entity.HasIndex(m => m.AuthorId);

            entity.Property(m => m.ChannelId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(m => m.AuthorId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(m => m.ReferencedMessageId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.HasOne(m => m.Channel)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Author)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.ReferencedMessage)
                .WithMany()
                .HasForeignKey(m => m.ReferencedMessageId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Embed Configuration
        modelBuilder.Entity<Embed>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MessageId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.HasOne(e => e.Footer).WithOne().HasForeignKey<Embed>("FooterId");
            entity.HasOne(e => e.Image).WithOne().HasForeignKey<Embed>("ImageId");
            entity.HasOne(e => e.Thumbnail).WithOne().HasForeignKey<Embed>("ThumbnailId");
            entity.HasOne(e => e.Video).WithOne().HasForeignKey<Embed>("VideoId");
            entity.HasOne(e => e.Provider).WithOne().HasForeignKey<Embed>("ProviderId");
            entity.HasOne(e => e.Author).WithOne().HasForeignKey<Embed>("AuthorId");
        });

        // Reaction Configuration
        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.MessageId, r.UserId, r.EmojiId, r.EmojiName }).IsUnique();

            entity.Property(r => r.MessageId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(r => r.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(r => r.EmojiId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);
        });

        // Role Configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.GuildId, r.Position });

            entity.Property(r => r.GuildId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.HasOne(r => r.Guild)
                .WithMany(g => g.Roles)
                .HasForeignKey(r => r.GuildId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Emoji Configuration
        modelBuilder.Entity<Emoji>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GuildId, e.Name });

            entity.Property(e => e.GuildId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.Property(e => e.CreatorId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.HasMany(e => e.RolesWhitelist)
                .WithMany()
                .UsingEntity(j => j.ToTable("EmojiRoleWhitelist"));
        });

        // Webhook Configuration
        modelBuilder.Entity<Webhook>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.Token).IsUnique();

            entity.Property(w => w.GuildId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.Property(w => w.ChannelId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(w => w.CreatorId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.Property(w => w.ApplicationId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);
        });

        // Audit Log Configuration
        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => new { a.GuildId, a.CreatedAt });

            entity.Property(a => a.GuildId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(a => a.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(a => a.TargetId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);
        });

        // VoiceState Configuration
        modelBuilder.Entity<VoiceState>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => new { v.UserId, v.GuildId }).IsUnique();

            entity.Property(v => v.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(v => v.GuildId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : (long?)null,
                    v => v.HasValue ? new Snowflake(v.Value) : (Snowflake?)null);

            entity.Property(v => v.ChannelId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // Invite Configuration
        modelBuilder.Entity<Invite>(entity =>
        {
            entity.HasKey(i => i.Code);
            entity.HasIndex(i => i.GuildId);
            entity.HasIndex(i => i.ExpiresAt);

            entity.Property(i => i.GuildId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(i => i.ChannelId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(i => i.InviterId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // PermissionOverwrite Configuration
        modelBuilder.Entity<PermissionOverwrite>(entity =>
        {
            entity.HasKey(po => po.Id);

            entity.Property(po => po.ChannelId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(po => po.TargetId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // ChannelPin Configuration
        modelBuilder.Entity<ChannelPin>(entity =>
        {
            entity.HasKey(cp => cp.Id);

            entity.Property(cp => cp.ChannelId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(cp => cp.MessageId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(cp => cp.PinnedBy)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // MessageMention Configuration
        modelBuilder.Entity<MessageMention>(entity =>
        {
            entity.HasKey(mm => mm.Id);

            entity.Property(mm => mm.MessageId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(mm => mm.UserId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // MessageRoleMention Configuration
        modelBuilder.Entity<MessageRoleMention>(entity =>
        {
            entity.HasKey(mrm => mrm.Id);

            entity.Property(mrm => mrm.MessageId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));

            entity.Property(mrm => mrm.RoleId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // Attachment Configuration
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.Property(a => a.MessageId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });

        // EmbedField Configuration
        modelBuilder.Entity<EmbedField>(entity =>
        {
            entity.HasKey(ef => ef.Id);
        });

        // MessageComponent Configuration
        modelBuilder.Entity<MessageComponent>(entity =>
        {
            entity.HasKey(mc => mc.Id);

            entity.Property(mc => mc.MessageId)
                .HasConversion(
                    v => v.Value,
                    v => new Snowflake(v));
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (entry.Entity is User user && user.Id.Value == 0)
                user.Id = _snowflakeGenerator.Generate();
            else if (entry.Entity is Guild guild && guild.Id.Value == 0)
                guild.Id = _snowflakeGenerator.Generate();
            else if (entry.Entity is Channel channel && channel.Id.Value == 0)
                channel.Id = _snowflakeGenerator.Generate();
            else if (entry.Entity is Message message && message.Id.Value == 0)
                message.Id = _snowflakeGenerator.Generate();
            else if (entry.Entity is Role role && role.Id.Value == 0)
                role.Id = _snowflakeGenerator.Generate();
            else if (entry.Entity is Emoji emoji && emoji.Id.Value == 0)
                emoji.Id = _snowflakeGenerator.Generate();
            else if (entry.Entity is Webhook webhook && webhook.Id.Value == 0)
                webhook.Id = _snowflakeGenerator.Generate();
            else if (entry.Entity is Attachment attachment && attachment.Id.Value == 0)
                attachment.Id = _snowflakeGenerator.Generate();
            else if (entry.Entity is AuditLogEntry auditLog && auditLog.Id.Value == 0)
                auditLog.Id = _snowflakeGenerator.Generate();
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
