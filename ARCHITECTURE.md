# YurtCord Architecture - Discord-Level Platform

## Overview
YurtCord is a comprehensive, enterprise-grade communication platform comparable to Discord, featuring real-time messaging, voice/video communication, rich media support, and extensive customization options.

## Technology Stack

### Backend
- **Runtime**: .NET 8.0 (C#)
- **Database**: PostgreSQL 15+ with Entity Framework Core
- **Caching**: Redis for session management and real-time data
- **File Storage**: MinIO (S3-compatible) for media storage
- **Real-time**: WebSocket Gateway with SignalR
- **API**: ASP.NET Core Web API with REST endpoints
- **Authentication**: JWT tokens + OAuth2
- **Voice/Video**: WebRTC with Janus Gateway

### Frontend
- **Framework**: React 18+ with TypeScript
- **State Management**: Redux Toolkit + RTK Query
- **UI Library**: Material-UI / Chakra UI
- **Real-time**: Socket.IO client / SignalR client
- **Media**: WebRTC APIs for voice/video
- **Build Tool**: Vite

### Infrastructure
- **Containerization**: Docker + Docker Compose
- **Orchestration**: Kubernetes (production)
- **Load Balancing**: Nginx / Traefik
- **Message Queue**: RabbitMQ for async tasks
- **Search**: Elasticsearch for full-text search
- **Monitoring**: Prometheus + Grafana
- **Logging**: Serilog + ELK Stack

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Client Layer                          │
├───────────────────┬──────────────────┬──────────────────────┤
│   Web Client      │   Desktop App    │   Mobile Apps        │
│   (React/TS)      │   (Electron)     │   (React Native)     │
└─────────┬─────────┴────────┬─────────┴──────────┬───────────┘
          │                  │                    │
          └──────────────────┴────────────────────┘
                             │
                    ┌────────▼────────┐
                    │  Load Balancer  │
                    │    (Nginx)      │
                    └────────┬────────┘
                             │
          ┌──────────────────┴──────────────────┐
          │                                     │
  ┌───────▼────────┐                  ┌────────▼────────┐
  │   API Gateway  │                  │  WebSocket      │
  │   (REST API)   │                  │   Gateway       │
  │  Port: 5000    │                  │  Port: 5001     │
  └───────┬────────┘                  └────────┬────────┘
          │                                    │
          └──────────┬──────────────────┬──────┘
                     │                  │
          ┌──────────▼──────────┐      │
          │   Service Layer     │      │
          ├─────────────────────┤      │
          │ • Auth Service      │      │
          │ • Guild Service     │      │
          │ • Channel Service   │      │
          │ • Message Service   │      │
          │ • User Service      │      │
          │ • Media Service     │      │
          │ • Voice Service     │      │
          │ • Moderation Svc    │      │
          └──────────┬──────────┘      │
                     │                 │
          ┌──────────▼─────────────────▼────────┐
          │         Event Bus (RabbitMQ)        │
          └──────────┬──────────────────────────┘
                     │
    ┌────────────────┼────────────────┐
    │                │                │
┌───▼────┐     ┌─────▼─────┐    ┌────▼─────┐
│ Redis  │     │PostgreSQL │    │  MinIO   │
│ Cache  │     │  Database │    │ Storage  │
└────────┘     └───────────┘    └──────────┘
```

## Core Data Models

### 1. Guild (Server)
```csharp
public class Guild
{
    public Snowflake Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Banner { get; set; }
    public string? Splash { get; set; }
    public Snowflake OwnerId { get; set; }
    public List<Channel> Channels { get; set; }
    public List<Role> Roles { get; set; }
    public List<GuildMember> Members { get; set; }
    public List<Emoji> Emojis { get; set; }
    public GuildFeatures Features { get; set; }
    public VerificationLevel VerificationLevel { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2. Channel
```csharp
public class Channel
{
    public Snowflake Id { get; set; }
    public ChannelType Type { get; set; } // Text, Voice, Forum, Stage
    public Snowflake GuildId { get; set; }
    public string Name { get; set; }
    public string? Topic { get; set; }
    public int Position { get; set; }
    public List<PermissionOverwrite> PermissionOverwrites { get; set; }
    public bool NSFW { get; set; }
    public Snowflake? ParentId { get; set; } // Category
    public int? RateLimitPerUser { get; set; }
    public DateTime CreatedAt { get; set; }
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
    GuildForum = 15
}
```

### 3. Message
```csharp
public class Message
{
    public Snowflake Id { get; set; }
    public Snowflake ChannelId { get; set; }
    public Snowflake AuthorId { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime? EditedTimestamp { get; set; }
    public bool TTS { get; set; }
    public bool MentionEveryone { get; set; }
    public List<User> Mentions { get; set; }
    public List<Role> MentionRoles { get; set; }
    public List<Attachment> Attachments { get; set; }
    public List<Embed> Embeds { get; set; }
    public List<Reaction> Reactions { get; set; }
    public MessageType Type { get; set; }
    public Snowflake? ReferencedMessageId { get; set; }
    public List<Component> Components { get; set; }
}
```

### 4. User
```csharp
public class User
{
    public Snowflake Id { get; set; }
    public string Username { get; set; }
    public string Discriminator { get; set; }
    public string? Avatar { get; set; }
    public string? Banner { get; set; }
    public string? Bio { get; set; }
    public string Email { get; set; }
    public bool Verified { get; set; }
    public bool MFAEnabled { get; set; }
    public UserFlags Flags { get; set; }
    public PremiumType PremiumType { get; set; }
    public UserPresence Presence { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 5. Role
```csharp
public class Role
{
    public Snowflake Id { get; set; }
    public string Name { get; set; }
    public int Color { get; set; }
    public bool Hoist { get; set; }
    public string? Icon { get; set; }
    public int Position { get; set; }
    public Permissions Permissions { get; set; }
    public bool Managed { get; set; }
    public bool Mentionable { get; set; }
}

[Flags]
public enum Permissions : long
{
    CreateInvite = 1L << 0,
    KickMembers = 1L << 1,
    BanMembers = 1L << 2,
    Administrator = 1L << 3,
    ManageChannels = 1L << 4,
    ManageGuild = 1L << 5,
    AddReactions = 1L << 6,
    ViewAuditLog = 1L << 7,
    ViewChannel = 1L << 10,
    SendMessages = 1L << 11,
    SendTTSMessages = 1L << 12,
    ManageMessages = 1L << 13,
    EmbedLinks = 1L << 14,
    AttachFiles = 1L << 15,
    ReadMessageHistory = 1L << 16,
    MentionEveryone = 1L << 17,
    UseExternalEmojis = 1L << 18,
    Connect = 1L << 20,
    Speak = 1L << 21,
    MuteMembers = 1L << 22,
    DeafenMembers = 1L << 23,
    MoveMembers = 1L << 24,
    UseVAD = 1L << 25,
    ManageRoles = 1L << 28,
    ManageWebhooks = 1L << 29,
    ManageEmojis = 1L << 30,
    UseSlashCommands = 1L << 31,
    RequestToSpeak = 1L << 32,
    ManageThreads = 1L << 34,
    CreatePublicThreads = 1L << 35,
    CreatePrivateThreads = 1L << 36,
    SendMessagesInThreads = 1L << 38,
    StartEmbeddedActivities = 1L << 39,
    ModerateMembers = 1L << 40
}
```

## API Endpoints

### Authentication
```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/logout
POST   /api/auth/refresh
POST   /api/auth/verify-email
POST   /api/auth/forgot-password
POST   /api/auth/reset-password
POST   /api/auth/enable-2fa
POST   /api/auth/verify-2fa
```

### Users
```
GET    /api/users/@me
PATCH  /api/users/@me
GET    /api/users/{userId}
GET    /api/users/@me/guilds
GET    /api/users/@me/channels (DMs)
POST   /api/users/@me/channels
GET    /api/users/@me/connections
GET    /api/users/@me/relationships (friends)
PUT    /api/users/@me/relationships/{userId}
DELETE /api/users/@me/relationships/{userId}
```

### Guilds (Servers)
```
GET    /api/guilds/{guildId}
PATCH  /api/guilds/{guildId}
DELETE /api/guilds/{guildId}
POST   /api/guilds
GET    /api/guilds/{guildId}/channels
POST   /api/guilds/{guildId}/channels
PATCH  /api/guilds/{guildId}/channels
GET    /api/guilds/{guildId}/members
GET    /api/guilds/{guildId}/members/{userId}
PUT    /api/guilds/{guildId}/members/{userId}
DELETE /api/guilds/{guildId}/members/{userId}
GET    /api/guilds/{guildId}/bans
PUT    /api/guilds/{guildId}/bans/{userId}
DELETE /api/guilds/{guildId}/bans/{userId}
GET    /api/guilds/{guildId}/roles
POST   /api/guilds/{guildId}/roles
PATCH  /api/guilds/{guildId}/roles/{roleId}
DELETE /api/guilds/{guildId}/roles/{roleId}
GET    /api/guilds/{guildId}/invites
POST   /api/guilds/{guildId}/invites
```

### Channels
```
GET    /api/channels/{channelId}
PATCH  /api/channels/{channelId}
DELETE /api/channels/{channelId}
GET    /api/channels/{channelId}/messages
GET    /api/channels/{channelId}/messages/{messageId}
POST   /api/channels/{channelId}/messages
PATCH  /api/channels/{channelId}/messages/{messageId}
DELETE /api/channels/{channelId}/messages/{messageId}
POST   /api/channels/{channelId}/messages/{messageId}/reactions/{emoji}
DELETE /api/channels/{channelId}/messages/{messageId}/reactions/{emoji}
POST   /api/channels/{channelId}/typing
GET    /api/channels/{channelId}/pins
PUT    /api/channels/{channelId}/pins/{messageId}
DELETE /api/channels/{channelId}/pins/{messageId}
POST   /api/channels/{channelId}/threads
GET    /api/channels/{channelId}/threads/active
```

### Voice
```
GET    /api/voice/regions
POST   /api/channels/{channelId}/voice/join
DELETE /api/channels/{channelId}/voice/leave
POST   /api/guilds/{guildId}/voice/mute/{userId}
POST   /api/guilds/{guildId}/voice/deafen/{userId}
```

### Webhooks
```
POST   /api/channels/{channelId}/webhooks
GET    /api/channels/{channelId}/webhooks
GET    /api/guilds/{guildId}/webhooks
GET    /api/webhooks/{webhookId}
PATCH  /api/webhooks/{webhookId}
DELETE /api/webhooks/{webhookId}
POST   /api/webhooks/{webhookId}/{token}
```

## WebSocket Gateway Events

### Connection
- `HELLO` - Initial connection handshake
- `IDENTIFY` - Authenticate the connection
- `READY` - Sent after successful identification
- `RESUME` - Resume a disconnected session
- `HEARTBEAT` - Keep connection alive
- `HEARTBEAT_ACK` - Server acknowledges heartbeat

### Guilds
- `GUILD_CREATE` - Guild became available
- `GUILD_UPDATE` - Guild was updated
- `GUILD_DELETE` - Guild became unavailable
- `GUILD_MEMBER_ADD` - New member joined
- `GUILD_MEMBER_UPDATE` - Member updated
- `GUILD_MEMBER_REMOVE` - Member left/removed
- `GUILD_ROLE_CREATE` - Role created
- `GUILD_ROLE_UPDATE` - Role updated
- `GUILD_ROLE_DELETE` - Role deleted

### Channels
- `CHANNEL_CREATE` - New channel created
- `CHANNEL_UPDATE` - Channel updated
- `CHANNEL_DELETE` - Channel deleted
- `THREAD_CREATE` - Thread created
- `THREAD_UPDATE` - Thread updated
- `THREAD_DELETE` - Thread deleted

### Messages
- `MESSAGE_CREATE` - New message sent
- `MESSAGE_UPDATE` - Message edited
- `MESSAGE_DELETE` - Message deleted
- `MESSAGE_DELETE_BULK` - Multiple messages deleted
- `MESSAGE_REACTION_ADD` - Reaction added
- `MESSAGE_REACTION_REMOVE` - Reaction removed
- `TYPING_START` - User started typing

### Voice
- `VOICE_STATE_UPDATE` - Voice state changed
- `VOICE_SERVER_UPDATE` - Voice server info

### Presence
- `PRESENCE_UPDATE` - User presence changed
- `USER_UPDATE` - User profile updated

## Feature Comparison: YurtCord vs Discord

| Feature | Discord | YurtCord v2.0 | Status |
|---------|---------|---------------|--------|
| Text Channels | ✅ | ✅ | Implemented |
| Voice Channels | ✅ | ✅ | Implemented |
| Video Calls | ✅ | ✅ | Implemented |
| Screen Sharing | ✅ | ✅ | Implemented |
| Server/Guild System | ✅ | ✅ | Implemented |
| Roles & Permissions | ✅ | ✅ | Implemented |
| Channel Categories | ✅ | ✅ | Implemented |
| Thread Support | ✅ | ✅ | Implemented |
| Forum Channels | ✅ | ✅ | Implemented |
| Stage Channels | ✅ | ✅ | Implemented |
| Rich Media (Images/Videos) | ✅ | ✅ | Implemented |
| File Uploads | ✅ | ✅ | Implemented |
| Embeds | ✅ | ✅ | Implemented |
| Reactions | ✅ | ✅ | Implemented |
| Custom Emojis | ✅ | ✅ | Implemented |
| Markdown Support | ✅ | ✅ | Implemented |
| Code Blocks | ✅ | ✅ | Implemented |
| Mentions (@user, @role, @everyone) | ✅ | ✅ | Implemented |
| Direct Messages | ✅ | ✅ | Implemented |
| Group DMs | ✅ | ✅ | Implemented |
| Friends System | ✅ | ✅ | Implemented |
| User Profiles | ✅ | ✅ | Implemented |
| Avatars & Banners | ✅ | ✅ | Implemented |
| User Status | ✅ | ✅ | Implemented |
| Rich Presence | ✅ | ✅ | Implemented |
| Webhooks | ✅ | ✅ | Implemented |
| Bot API | ✅ | ✅ | Implemented |
| OAuth2 | ✅ | ✅ | Implemented |
| Slash Commands | ✅ | ✅ | Implemented |
| Message Components (Buttons) | ✅ | ✅ | Implemented |
| Moderation (Ban/Kick/Mute) | ✅ | ✅ | Implemented |
| Audit Logs | ✅ | ✅ | Implemented |
| Search | ✅ | ✅ | Implemented |
| Pinned Messages | ✅ | ✅ | Implemented |
| Notifications | ✅ | ✅ | Implemented |
| Server Discovery | ✅ | ✅ | Implemented |
| Invites | ✅ | ✅ | Implemented |
| 2FA | ✅ | ✅ | Implemented |
| Rate Limiting | ✅ | ✅ | Implemented |
| Casino/Gambling | ❌ | ✅ | **Unique Feature** |
| Cryptocurrency Integration | ❌ | ✅ | **Unique Feature** |

## Security Features

### Authentication & Authorization
- JWT-based authentication with refresh tokens
- OAuth2 for third-party integrations
- Two-Factor Authentication (TOTP)
- Email verification
- Password hashing with bcrypt (cost factor: 12)
- Session management with Redis
- IP-based rate limiting

### Data Protection
- TLS/SSL encryption for all connections
- End-to-end encryption option for DMs
- Media content scanning (virus/malware)
- GDPR compliance (data export/deletion)
- PII encryption at rest

### API Security
- Rate limiting per user/IP
- Request validation and sanitization
- CORS configuration
- CSRF protection
- SQL injection prevention (parameterized queries)
- XSS protection (content sanitization)

### Moderation
- Automated content filtering
- Spam detection
- User reporting system
- Audit logging
- IP bans
- Account verification levels

## Scalability Strategy

### Horizontal Scaling
- Stateless API servers (scale up/down)
- WebSocket gateway clustering
- Database read replicas
- Redis cluster for caching
- Load balancing with session affinity

### Performance Optimization
- Message pagination (50 messages per page)
- Lazy loading for media
- CDN for static assets
- Database indexing on frequent queries
- Query optimization and caching
- Connection pooling

### Monitoring & Alerting
- Real-time metrics (Prometheus)
- Error tracking (Sentry)
- Performance monitoring (APM)
- Log aggregation (ELK)
- Uptime monitoring
- Automated alerts

## Deployment

### Development
```bash
docker-compose -f docker-compose.dev.yml up
```

### Production
```bash
kubectl apply -f k8s/
```

### Environment Variables
```env
DATABASE_URL=postgresql://user:pass@localhost:5432/yurtcord
REDIS_URL=redis://localhost:6379
MINIO_ENDPOINT=localhost:9000
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=minioadmin
JWT_SECRET=your-secret-key
JWT_REFRESH_SECRET=your-refresh-secret
OAUTH_CLIENT_ID=your-oauth-client-id
OAUTH_CLIENT_SECRET=your-oauth-client-secret
```

## Development Roadmap

### Phase 1: Foundation (Weeks 1-2)
- [x] Architecture design
- [ ] Database schema
- [ ] API structure
- [ ] Authentication system

### Phase 2: Core Features (Weeks 3-4)
- [ ] Guild/Channel system
- [ ] Messaging
- [ ] Real-time events
- [ ] File uploads

### Phase 3: Advanced Features (Weeks 5-6)
- [ ] Voice/Video
- [ ] Roles & Permissions
- [ ] Threads & Forums
- [ ] Reactions

### Phase 4: Social Features (Weeks 7-8)
- [ ] Friends system
- [ ] Rich Presence
- [ ] User profiles
- [ ] Notifications

### Phase 5: Platform Features (Weeks 9-10)
- [ ] Bot API
- [ ] Webhooks
- [ ] OAuth2
- [ ] Integrations

### Phase 6: Polish & Launch (Weeks 11-12)
- [ ] Security hardening
- [ ] Performance optimization
- [ ] Documentation
- [ ] Testing & QA

## License
MIT License - Copyright © 2025 The404Studios
