using YurtCord.Core.Common;
using YurtCord.Core.Entities;

namespace YurtCord.Infrastructure.Data;

/// <summary>
/// Seeds the database with example data for development and testing
/// </summary>
public class DatabaseSeeder(YurtCordDbContext context, SnowflakeGenerator snowflakeGenerator)
{
    private readonly YurtCordDbContext _context = context;
    private readonly SnowflakeGenerator _snowflakeGenerator = snowflakeGenerator;

    public async Task SeedAsync()
    {
        // Only seed if database is empty
        if (_context.Users.Any())
        {
            return;
        }

        Console.WriteLine("🌱 Seeding database with example data...");

        // Create users
        var users = await CreateUsersAsync();
        Console.WriteLine($"✅ Created {users.Count} users");

        // Create guilds
        var guilds = await CreateGuildsAsync(users);
        Console.WriteLine($"✅ Created {guilds.Count} guilds");

        // Create channels
        await CreateChannelsAsync(guilds);
        Console.WriteLine("✅ Created channels for guilds");

        // Create messages
        await CreateMessagesAsync(users, guilds);
        Console.WriteLine("✅ Created example messages");

        Console.WriteLine("🎉 Database seeding completed!");
    }

    private async Task<List<User>> CreateUsersAsync()
    {
        var users = new List<User>
        {
            new User
            {
                Id = _snowflakeGenerator.Generate(),
                Username = "admin",
                Discriminator = "0001",
                Email = "admin@yurtcord.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", 12),
                Avatar = null,
                Verified = true,
                MfaEnabled = false,
                Bio = "YurtCord Administrator",
                Flags = UserFlags.Staff | UserFlags.VerifiedBot,
                PremiumType = PremiumType.NitroClassic,
                PublicFlags = UserFlags.Staff,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = _snowflakeGenerator.Generate(),
                Username = "alice",
                Discriminator = "0001",
                Email = "alice@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                Avatar = null,
                Verified = true,
                MfaEnabled = false,
                Bio = "Hey there! I'm Alice 👋",
                Flags = UserFlags.None,
                PremiumType = PremiumType.None,
                PublicFlags = UserFlags.None,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = _snowflakeGenerator.Generate(),
                Username = "bob",
                Discriminator = "0001",
                Email = "bob@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                Avatar = null,
                Verified = true,
                MfaEnabled = false,
                Bio = "Software developer and gamer",
                Flags = UserFlags.None,
                PremiumType = PremiumType.Nitro,
                PublicFlags = UserFlags.EarlySupporter,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = _snowflakeGenerator.Generate(),
                Username = "charlie",
                Discriminator = "0001",
                Email = "charlie@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                Avatar = null,
                Verified = true,
                MfaEnabled = false,
                Bio = "Music lover 🎵",
                Flags = UserFlags.None,
                PremiumType = PremiumType.None,
                PublicFlags = UserFlags.None,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = _snowflakeGenerator.Generate(),
                Username = "diana",
                Discriminator = "0001",
                Email = "diana@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", 12),
                Avatar = null,
                Verified = true,
                MfaEnabled = false,
                Bio = "Designer and artist 🎨",
                Flags = UserFlags.None,
                PremiumType = PremiumType.None,
                PublicFlags = UserFlags.HypeSquadBravery,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Users.AddRange(users);

        // Create presence for each user
        foreach (var user in users)
        {
            _context.UserPresences.Add(new UserPresence
            {
                UserId = user.Id,
                Status = PresenceStatus.Online,
                CustomStatus = user.Username == "admin" ? "Administrating" : null,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return users;
    }

    private async Task<List<Guild>> CreateGuildsAsync(List<User> users)
    {
        var admin = users.First(u => u.Username == "admin");
        var alice = users.First(u => u.Username == "alice");
        var bob = users.First(u => u.Username == "bob");

        var guilds = new List<Guild>
        {
            new Guild
            {
                Id = _snowflakeGenerator.Generate(),
                Name = "YurtCord Official",
                Description = "Official YurtCord server - Welcome!",
                OwnerId = admin.Id,
                Icon = null,
                Banner = null,
                VerificationLevel = VerificationLevel.Low,
                DefaultMessageNotifications = DefaultMessageNotificationLevel.OnlyMentions,
                ExplicitContentFilter = ExplicitContentFilterLevel.MembersWithoutRoles,
                AfkTimeout = 300,
                Features = GuildFeatures.Community | GuildFeatures.WelcomeScreenEnabled,
                CreatedAt = DateTime.UtcNow
            },
            new Guild
            {
                Id = _snowflakeGenerator.Generate(),
                Name = "Gaming Hub",
                Description = "A place for gamers to connect and play together",
                OwnerId = alice.Id,
                Icon = null,
                Banner = null,
                VerificationLevel = VerificationLevel.Low,
                DefaultMessageNotifications = DefaultMessageNotificationLevel.AllMessages,
                ExplicitContentFilter = ExplicitContentFilterLevel.Disabled,
                AfkTimeout = 600,
                Features = GuildFeatures.None,
                CreatedAt = DateTime.UtcNow
            },
            new Guild
            {
                Id = _snowflakeGenerator.Generate(),
                Name = "Dev Community",
                Description = "Software developers helping each other",
                OwnerId = bob.Id,
                Icon = null,
                Banner = null,
                VerificationLevel = VerificationLevel.Medium,
                DefaultMessageNotifications = DefaultMessageNotificationLevel.OnlyMentions,
                ExplicitContentFilter = ExplicitContentFilterLevel.AllMembers,
                AfkTimeout = 300,
                Features = GuildFeatures.Community,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Guilds.AddRange(guilds);
        await _context.SaveChangesAsync();

        // Create roles and members for each guild
        foreach (var guild in guilds)
        {
            await CreateGuildRolesAsync(guild);
            await CreateGuildMembersAsync(guild, users);
        }

        return guilds;
    }

    private async Task CreateGuildRolesAsync(Guild guild)
    {
        var roles = new List<Role>
        {
            // @everyone role (same ID as guild)
            new Role
            {
                Id = guild.Id,
                GuildId = guild.Id,
                Name = "@everyone",
                Color = 0,
                Hoist = false,
                Position = 0,
                Permissions = Permissions.ViewChannel | Permissions.SendMessages |
                             Permissions.ReadMessageHistory | Permissions.Connect |
                             Permissions.Speak | Permissions.UseVoiceActivity,
                Managed = false,
                Mentionable = false,
                CreatedAt = DateTime.UtcNow
            },
            // Admin role
            new Role
            {
                Id = _snowflakeGenerator.Generate(),
                GuildId = guild.Id,
                Name = "Admin",
                Color = 16711680, // Red
                Hoist = true,
                Position = 10,
                Permissions = Permissions.Administrator,
                Managed = false,
                Mentionable = true,
                CreatedAt = DateTime.UtcNow
            },
            // Moderator role
            new Role
            {
                Id = _snowflakeGenerator.Generate(),
                GuildId = guild.Id,
                Name = "Moderator",
                Color = 3447003, // Blue
                Hoist = true,
                Position = 5,
                Permissions = Permissions.KickMembers | Permissions.BanMembers |
                             Permissions.ManageMessages | Permissions.ViewAuditLog |
                             Permissions.MuteMembers | Permissions.DeafenMembers,
                Managed = false,
                Mentionable = true,
                CreatedAt = DateTime.UtcNow
            },
            // Member role
            new Role
            {
                Id = _snowflakeGenerator.Generate(),
                GuildId = guild.Id,
                Name = "Member",
                Color = 10070709, // Green
                Hoist = false,
                Position = 1,
                Permissions = Permissions.ViewChannel | Permissions.SendMessages |
                             Permissions.EmbedLinks | Permissions.AttachFiles |
                             Permissions.ReadMessageHistory | Permissions.UseExternalEmojis |
                             Permissions.AddReactions | Permissions.Connect |
                             Permissions.Speak | Permissions.Stream,
                Managed = false,
                Mentionable = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Roles.AddRange(roles);
        await _context.SaveChangesAsync();
    }

    private async Task CreateGuildMembersAsync(Guild guild, List<User> users)
    {
        var adminRole = _context.Roles
            .First(r => r.GuildId == guild.Id && r.Name == "Admin");
        var memberRole = _context.Roles
            .First(r => r.GuildId == guild.Id && r.Name == "Member");

        // Add all users as members
        foreach (var user in users)
        {
            var member = new GuildMember
            {
                GuildId = guild.Id,
                UserId = user.Id,
                Nickname = null,
                JoinedAt = DateTime.UtcNow,
                Deaf = false,
                Mute = false,
                Pending = false
            };

            _context.GuildMembers.Add(member);
            await _context.SaveChangesAsync();

            // Owner gets admin role
            if (user.Id == guild.OwnerId)
            {
                _context.Database.ExecuteSqlRaw(
                    "INSERT INTO \"GuildMemberRole\" (\"MembersGuildId\", \"MembersUserId\", \"RolesId\") VALUES ({0}, {1}, {2})",
                    guild.Id.Value, user.Id.Value, adminRole.Id.Value);
            }
            else
            {
                // Others get member role
                _context.Database.ExecuteSqlRaw(
                    "INSERT INTO \"GuildMemberRole\" (\"MembersGuildId\", \"MembersUserId\", \"RolesId\") VALUES ({0}, {1}, {2})",
                    guild.Id.Value, user.Id.Value, memberRole.Id.Value);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task CreateChannelsAsync(List<Guild> guilds)
    {
        foreach (var guild in guilds)
        {
            // Create categories
            var generalCategory = new Channel
            {
                Id = _snowflakeGenerator.Generate(),
                Type = ChannelType.GuildCategory,
                GuildId = guild.Id,
                Name = "General",
                Position = 0,
                CreatedAt = DateTime.UtcNow
            };

            var voiceCategory = new Channel
            {
                Id = _snowflakeGenerator.Generate(),
                Type = ChannelType.GuildCategory,
                GuildId = guild.Id,
                Name = "Voice Channels",
                Position = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Channels.AddRange(generalCategory, voiceCategory);
            await _context.SaveChangesAsync();

            // Create text channels
            var channels = new List<Channel>
            {
                new Channel
                {
                    Id = _snowflakeGenerator.Generate(),
                    Type = ChannelType.GuildText,
                    GuildId = guild.Id,
                    ParentId = generalCategory.Id,
                    Name = "welcome",
                    Topic = "Welcome to the server! Read the rules and introduce yourself.",
                    Position = 0,
                    Nsfw = false,
                    RateLimitPerUser = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Channel
                {
                    Id = _snowflakeGenerator.Generate(),
                    Type = ChannelType.GuildText,
                    GuildId = guild.Id,
                    ParentId = generalCategory.Id,
                    Name = "general",
                    Topic = "General discussion about anything",
                    Position = 1,
                    Nsfw = false,
                    RateLimitPerUser = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Channel
                {
                    Id = _snowflakeGenerator.Generate(),
                    Type = ChannelType.GuildText,
                    GuildId = guild.Id,
                    ParentId = generalCategory.Id,
                    Name = "memes",
                    Topic = "Share your favorite memes and funny content",
                    Position = 2,
                    Nsfw = false,
                    RateLimitPerUser = 5,
                    CreatedAt = DateTime.UtcNow
                },
                // Voice channels
                new Channel
                {
                    Id = _snowflakeGenerator.Generate(),
                    Type = ChannelType.GuildVoice,
                    GuildId = guild.Id,
                    ParentId = voiceCategory.Id,
                    Name = "General Voice",
                    Position = 0,
                    Bitrate = 64000,
                    UserLimit = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new Channel
                {
                    Id = _snowflakeGenerator.Generate(),
                    Type = ChannelType.GuildVoice,
                    GuildId = guild.Id,
                    ParentId = voiceCategory.Id,
                    Name = "Gaming",
                    Position = 1,
                    Bitrate = 96000,
                    UserLimit = 10,
                    CreatedAt = DateTime.UtcNow
                },
                new Channel
                {
                    Id = _snowflakeGenerator.Generate(),
                    Type = ChannelType.GuildVoice,
                    GuildId = guild.Id,
                    ParentId = voiceCategory.Id,
                    Name = "AFK",
                    Position = 2,
                    Bitrate = 64000,
                    UserLimit = 0,
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.Channels.AddRange(channels);
            await _context.SaveChangesAsync();
        }
    }

    private async Task CreateMessagesAsync(List<User> users, List<Guild> guilds)
    {
        var admin = users.First(u => u.Username == "admin");
        var alice = users.First(u => u.Username == "alice");
        var bob = users.First(u => u.Username == "bob");

        foreach (var guild in guilds)
        {
            var generalChannel = _context.Channels
                .First(c => c.GuildId == guild.Id && c.Name == "general");

            var welcomeChannel = _context.Channels
                .First(c => c.GuildId == guild.Id && c.Name == "welcome");

            // Welcome messages
            var welcomeMessages = new List<Message>
            {
                new Message
                {
                    Id = _snowflakeGenerator.Generate(),
                    ChannelId = welcomeChannel.Id,
                    AuthorId = guild.OwnerId,
                    Content = $"Welcome to {guild.Name}! 🎉",
                    Timestamp = DateTime.UtcNow.AddHours(-24),
                    Type = MessageType.Default,
                    Pinned = true,
                    MentionEveryone = false,
                    Tts = false
                },
                new Message
                {
                    Id = _snowflakeGenerator.Generate(),
                    ChannelId = welcomeChannel.Id,
                    AuthorId = guild.OwnerId,
                    Content = "Please read the rules and be respectful to everyone.",
                    Timestamp = DateTime.UtcNow.AddHours(-24).AddMinutes(1),
                    Type = MessageType.Default,
                    Pinned = false,
                    MentionEveryone = false,
                    Tts = false
                }
            };

            // General chat messages
            var generalMessages = new List<Message>
            {
                new Message
                {
                    Id = _snowflakeGenerator.Generate(),
                    ChannelId = generalChannel.Id,
                    AuthorId = alice.Id,
                    Content = "Hey everyone! How's it going?",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    Type = MessageType.Default,
                    MentionEveryone = false,
                    Tts = false
                },
                new Message
                {
                    Id = _snowflakeGenerator.Generate(),
                    ChannelId = generalChannel.Id,
                    AuthorId = bob.Id,
                    Content = "All good here! Working on some new code.",
                    Timestamp = DateTime.UtcNow.AddHours(-2).AddMinutes(5),
                    Type = MessageType.Default,
                    MentionEveryone = false,
                    Tts = false
                },
                new Message
                {
                    Id = _snowflakeGenerator.Generate(),
                    ChannelId = generalChannel.Id,
                    AuthorId = admin.Id,
                    Content = "Remember to check out the new features we added!",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    Type = MessageType.Default,
                    MentionEveryone = false,
                    Tts = false
                }
            };

            _context.Messages.AddRange(welcomeMessages);
            _context.Messages.AddRange(generalMessages);
            await _context.SaveChangesAsync();

            // Add some reactions
            var firstMessage = generalMessages.First();
            _context.Reactions.AddRange(
                new Reaction
                {
                    MessageId = firstMessage.Id,
                    UserId = bob.Id,
                    EmojiName = "👍",
                    CreatedAt = DateTime.UtcNow.AddHours(-1).AddMinutes(50)
                },
                new Reaction
                {
                    MessageId = firstMessage.Id,
                    UserId = admin.Id,
                    EmojiName = "👍",
                    CreatedAt = DateTime.UtcNow.AddHours(-1).AddMinutes(45)
                }
            );

            await _context.SaveChangesAsync();
        }
    }
}
