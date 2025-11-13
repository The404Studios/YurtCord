# YurtCord REST API Documentation

Complete API reference for YurtCord - A Discord-like communication platform.

**Base URL**: `http://localhost:5000/api`
**WebSocket Gateway**: `ws://localhost:5000/gateway`
**Interactive Docs**: `http://localhost:5000/swagger`

---

## Table of Contents

- [Authentication](#authentication)
- [Rate Limiting](#rate-limiting)
- [Error Handling](#error-handling)
- [Authentication Endpoints](#authentication-endpoints)
- [User Endpoints](#user-endpoints)
- [Guild (Server) Endpoints](#guild-server-endpoints)
- [Channel Endpoints](#channel-endpoints)
- [Message Endpoints](#message-endpoints)
- [Voice Endpoints](#voice-endpoints)
- [WebSocket Gateway](#websocket-gateway)

---

## Authentication

YurtCord uses **JWT (JSON Web Tokens)** for authentication.

### Getting a Token

```http
POST /api/auth/register
POST /api/auth/login
```

### Using the Token

Include the JWT token in the `Authorization` header for all authenticated requests:

```http
Authorization: Bearer <your-jwt-token>
```

**Token Expiration**: 7 days

---

## Rate Limiting

**Global Rate Limit**: 100 requests per minute per user/IP

When rate limited, you'll receive:
```json
{
  "error": "Too many requests. Please try again later."
}
```
**Status Code**: `429 Too Many Requests`

---

## Error Handling

### Standard Error Response

```json
{
  "error": "Error message describing what went wrong"
}
```

### HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| `200` | OK | Request succeeded |
| `201` | Created | Resource created successfully |
| `204` | No Content | Request succeeded with no response body |
| `400` | Bad Request | Invalid request parameters |
| `401` | Unauthorized | Missing or invalid authentication |
| `403` | Forbidden | Insufficient permissions |
| `404` | Not Found | Resource doesn't exist |
| `429` | Too Many Requests | Rate limit exceeded |
| `500` | Internal Server Error | Server error |

---

## Authentication Endpoints

### Register a New User

Creates a new user account.

```http
POST /api/auth/register
```

**Request Body**:
```json
{
  "username": "john",
  "email": "john@example.com",
  "password": "Password123!"
}
```

**Validation**:
- Username: 2-32 characters
- Email: Valid email format
- Password: Minimum 8 characters

**Response** `200 OK`:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "123456789012345678",
    "username": "john",
    "discriminator": "0001",
    "email": "john@example.com",
    "avatar": null,
    "verified": false
  }
}
```

---

### Login

Authenticate with existing credentials.

```http
POST /api/auth/login
```

**Request Body**:
```json
{
  "email": "john@example.com",
  "password": "Password123!"
}
```

**Response** `200 OK`:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "123456789012345678",
    "username": "john",
    "discriminator": "0001",
    "email": "john@example.com",
    "avatar": null,
    "verified": false
  }
}
```

---

### Get Current User

Get authenticated user's information.

```http
GET /api/auth/me
```

**Headers**: `Authorization: Bearer <token>`

**Response** `200 OK`:
```json
{
  "id": "123456789012345678",
  "username": "john",
  "discriminator": "0001",
  "email": "john@example.com",
  "avatar": null,
  "banner": null,
  "bio": null,
  "verified": false,
  "mfaEnabled": false,
  "flags": "None",
  "premiumType": "None",
  "createdAt": "2025-01-15T10:30:00Z"
}
```

---

## User Endpoints

### Get User by ID

Get any user's public profile.

```http
GET /api/users/{userId}
```

**Response** `200 OK`:
```json
{
  "id": "123456789012345678",
  "username": "john",
  "discriminator": "0001",
  "tag": "john#0001",
  "avatar": null,
  "banner": null,
  "accentColor": null,
  "bio": "Hello, I'm John!",
  "flags": "None",
  "premiumType": "None",
  "publicFlags": "None",
  "createdAt": "2025-01-15T10:30:00Z"
}
```

---

### Update Current User

Update your profile.

```http
PATCH /api/users/@me
```

**Request Body**:
```json
{
  "username": "newname",
  "avatar": "data:image/png;base64,...",
  "banner": "data:image/png;base64,...",
  "accentColor": 5814783,
  "bio": "My new bio"
}
```

**All fields are optional**

**Response** `200 OK`: Updated user object

---

### Get Current User's Guilds

List all servers the user is a member of.

```http
GET /api/users/@me/guilds
```

**Response** `200 OK`:
```json
[
  {
    "id": "987654321098765432",
    "name": "My Server",
    "icon": null,
    "banner": null,
    "owner": true,
    "permissions": "0",
    "features": "None"
  }
]
```

---

### Leave a Guild

Leave a server (cannot leave if owner).

```http
DELETE /api/users/@me/guilds/{guildId}
```

**Response** `204 No Content`

---

### Get DM Channels

Get all direct message channels.

```http
GET /api/users/@me/channels
```

**Response** `200 OK`:
```json
[
  {
    "id": "111222333444555666",
    "type": "DM",
    "lastMessageAt": "2025-01-15T12:00:00Z",
    "recipients": []
  }
]
```

---

### Create DM Channel

Start a direct message conversation.

```http
POST /api/users/@me/channels
```

**Request Body**:
```json
{
  "recipientId": "123456789012345678"
}
```

**Response** `201 Created`:
```json
{
  "id": "111222333444555666",
  "type": "DM",
  "recipients": [
    {
      "id": "123456789012345678",
      "username": "john",
      "discriminator": "0001",
      "avatar": null
    }
  ]
}
```

---

### Update Presence

Update your online status.

```http
PATCH /api/users/@me/presence
```

**Request Body**:
```json
{
  "status": "online",
  "customStatus": "Playing games"
}
```

**Status Options**: `online`, `idle`, `dnd`, `invisible`

**Response** `200 OK`:
```json
{
  "status": "online",
  "customStatus": "Playing games",
  "updatedAt": "2025-01-15T12:00:00Z"
}
```

---

## Guild (Server) Endpoints

### Get User's Guilds

```http
GET /api/guilds/@me
```

**Response** `200 OK`: Array of guild objects with member count

---

### Get Guild

Get detailed guild information.

```http
GET /api/guilds/{guildId}
```

**Response** `200 OK`:
```json
{
  "id": "987654321098765432",
  "name": "My Server",
  "description": "Welcome to my server!",
  "icon": null,
  "banner": null,
  "splash": null,
  "ownerId": "123456789012345678",
  "verificationLevel": "None",
  "features": "None",
  "channels": [...],
  "roles": [...]
}
```

---

### Create Guild

Create a new server.

```http
POST /api/guilds
```

**Request Body**:
```json
{
  "name": "My New Server",
  "description": "Server description"
}
```

**Response** `201 Created`: Guild object

**Default Channels Created**:
- `#general` (text channel)
- `General Voice` (voice channel)

**Default Roles Created**:
- `@everyone` with basic permissions

---

### Update Guild

Update server settings (requires `MANAGE_GUILD` permission).

```http
PATCH /api/guilds/{guildId}
```

**Request Body**:
```json
{
  "name": "Updated Name",
  "description": "Updated description",
  "icon": "data:image/png;base64,...",
  "banner": "data:image/png;base64,..."
}
```

**All fields are optional**

**Response** `200 OK`: Updated guild object

---

### Delete Guild

Delete a server (owner only).

```http
DELETE /api/guilds/{guildId}
```

**Response** `204 No Content`

---

### Create Channel

Create a channel in a guild.

```http
POST /api/guilds/{guildId}/channels
```

**Request Body**:
```json
{
  "type": "GuildText",
  "name": "new-channel",
  "topic": "Channel topic",
  "position": 0,
  "nsfw": false,
  "parentId": null,
  "rateLimitPerUser": 0
}
```

**Channel Types**:
- `GuildText` (0)
- `GuildVoice` (2)
- `GuildCategory` (4)
- `GuildNews` (5)
- `GuildStageVoice` (13)
- `GuildForum` (15)

**Voice Channel Additional Fields**:
```json
{
  "bitrate": 64000,
  "userLimit": 10
}
```

**Response** `201 Created`: Channel object

---

### Create Role

Create a role in a guild.

```http
POST /api/guilds/{guildId}/roles
```

**Request Body**:
```json
{
  "name": "Moderator",
  "color": 5814783,
  "hoist": true,
  "position": 1,
  "permissions": "KickMembers, BanMembers, ManageMessages",
  "mentionable": true
}
```

**Response** `200 OK`: Role object

---

## Channel Endpoints

### Get Channel

Get channel details.

```http
GET /api/channels/{channelId}
```

**Response** `200 OK`:
```json
{
  "id": "111222333444555666",
  "type": "GuildText",
  "guildId": "987654321098765432",
  "position": 0,
  "name": "general",
  "topic": "General discussion",
  "nsfw": false,
  "lastMessageAt": "2025-01-15T12:00:00Z",
  "rateLimitPerUser": 0,
  "parentId": null
}
```

---

### Update Channel

Update channel settings.

```http
PATCH /api/channels/{channelId}
```

**Request Body** (all fields optional):
```json
{
  "name": "new-name",
  "topic": "New topic",
  "position": 1,
  "nsfw": false,
  "rateLimitPerUser": 5,
  "bitrate": 96000,
  "userLimit": 20,
  "parentId": "categoryId"
}
```

**Response** `200 OK`: Updated channel object

---

### Delete Channel

Delete a channel.

```http
DELETE /api/channels/{channelId}
```

**Response** `204 No Content`

---

### Get Channel Permissions

Get your permissions for a channel.

```http
GET /api/channels/{channelId}/permissions
```

**Response** `200 OK`:
```json
{
  "permissions": "ViewChannel, SendMessages, ReadMessageHistory",
  "permissionsValue": 3072,
  "overwrites": [
    {
      "id": "roleOrUserId",
      "type": "Role",
      "allow": "SendMessages",
      "deny": "None"
    }
  ]
}
```

---

### Trigger Typing Indicator

Show that you're typing in a channel.

```http
POST /api/channels/{channelId}/typing
```

**Response** `204 No Content`

**Note**: Typing indicator expires after 10 seconds

---

### Get Pinned Messages

Get all pinned messages in a channel.

```http
GET /api/channels/{channelId}/pins
```

**Response** `200 OK`: Array of message objects (max 50)

---

### Pin Message

Pin a message in a channel.

```http
PUT /api/channels/{channelId}/pins/{messageId}
```

**Requires**: `MANAGE_MESSAGES` permission

**Response** `204 No Content`

---

### Unpin Message

Unpin a message.

```http
DELETE /api/channels/{channelId}/pins/{messageId}
```

**Requires**: `MANAGE_MESSAGES` permission

**Response** `204 No Content`

---

## Message Endpoints

### Get Messages

Get messages from a channel with pagination.

```http
GET /api/channels/{channelId}/messages?limit=50&before=messageId&after=messageId
```

**Query Parameters**:
- `limit`: Number of messages (1-100, default: 50)
- `before`: Get messages before this message ID
- `after`: Get messages after this message ID

**Response** `200 OK`:
```json
[
  {
    "id": "222333444555666777",
    "channelId": "111222333444555666",
    "author": {
      "id": "123456789012345678",
      "username": "john",
      "discriminator": "0001",
      "avatar": null
    },
    "content": "Hello, world!",
    "timestamp": "2025-01-15T12:00:00Z",
    "editedTimestamp": null,
    "tts": false,
    "mentionEveryone": false,
    "mentions": [],
    "reactions": [
      {
        "emoji": "👍",
        "count": 5,
        "me": true
      }
    ],
    "type": "Default"
  }
]
```

---

### Get Message

Get a specific message.

```http
GET /api/channels/{channelId}/messages/{messageId}
```

**Response** `200 OK`: Message object

---

### Send Message

Send a message to a channel.

```http
POST /api/channels/{channelId}/messages
```

**Request Body**:
```json
{
  "content": "Hello, world!",
  "tts": false,
  "messageReference": "replyToMessageId"
}
```

**Validation**:
- Content: 1-2000 characters
- TTS: Text-to-speech (optional)
- Message Reference: Reply to message (optional)

**Response** `201 Created`: Created message object

---

### Edit Message

Edit your own message.

```http
PATCH /api/channels/{channelId}/messages/{messageId}
```

**Request Body**:
```json
{
  "content": "Updated message content"
}
```

**Response** `200 OK`: Updated message object with `editedTimestamp`

---

### Delete Message

Delete a message.

```http
DELETE /api/channels/{channelId}/messages/{messageId}
```

**Permissions**:
- Can delete own messages
- Can delete others' messages with `MANAGE_MESSAGES` permission

**Response** `204 No Content`

---

### Add Reaction

React to a message with an emoji.

```http
PUT /api/channels/{channelId}/messages/{messageId}/reactions/{emoji}/@me
```

**Example**:
```http
PUT /api/channels/123/messages/456/reactions/👍/@me
PUT /api/channels/123/messages/456/reactions/custom_emoji_name/@me
```

**Response** `204 No Content`

---

### Remove Reaction

Remove your reaction from a message.

```http
DELETE /api/channels/{channelId}/messages/{messageId}/reactions/{emoji}/@me
```

**Response** `204 No Content`

---

## Voice Endpoints

### Get Voice Regions

Get available voice server regions.

```http
GET /api/voice/regions
```

**Response** `200 OK`:
```json
[
  {
    "id": "us-west",
    "name": "US West",
    "optimal": true,
    "deprecated": false,
    "custom": false
  },
  {
    "id": "us-east",
    "name": "US East",
    "optimal": false,
    "deprecated": false,
    "custom": false
  }
]
```

---

### Join Voice Channel

Join a voice channel and get connection info.

```http
POST /api/voice/channels/{channelId}/join
```

**Response** `200 OK`:
```json
{
  "token": "base64EncodedToken",
  "sessionId": "unique-session-id",
  "channelId": "111222333444555666",
  "userId": "123456789012345678",
  "iceServers": {
    "iceServers": [
      {
        "urls": ["stun:stun.l.google.com:19302"]
      },
      {
        "urls": ["turn:your-turn-server.com:3478"],
        "username": "yurtcord",
        "credential": "credential"
      }
    ]
  }
}
```

**Use this response to establish WebRTC connection**

---

### Leave Voice Channel

Leave a voice channel.

```http
POST /api/voice/channels/{channelId}/leave
```

**Response** `204 No Content`

---

### Update Voice State

Update your voice state (mute, deafen, video).

```http
PATCH /api/voice/state
```

**Request Body**:
```json
{
  "selfMute": true,
  "selfDeaf": false,
  "selfVideo": false
}
```

**Response** `200 OK`:
```json
{
  "userId": "123456789012345678",
  "channelId": "111222333444555666",
  "selfMute": true,
  "selfDeaf": false,
  "selfVideo": false,
  "mute": false,
  "deaf": false
}
```

---

### Get Users in Voice Channel

Get all users currently in a voice channel.

```http
GET /api/voice/channels/{channelId}/users
```

**Response** `200 OK`:
```json
[
  {
    "userId": "123456789012345678",
    "username": "john",
    "discriminator": "0001",
    "avatar": null,
    "sessionId": "session-id",
    "selfMute": false,
    "selfDeaf": false,
    "selfVideo": false,
    "mute": false,
    "deaf": false,
    "suppress": false
  }
]
```

---

## WebSocket Gateway

Real-time events via SignalR WebSocket connection.

**Connection URL**: `ws://localhost:5000/gateway`

### Connecting

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/gateway", {
    accessTokenFactory: () => yourJwtToken
  })
  .build();

await connection.start();
```

---

### Events to Receive

#### Ready

Sent when connection is established.

```javascript
connection.on("Ready", (data) => {
  console.log("Connected!", data);
  // data = { user: {...}, sessionId: "..." }
});
```

---

#### MessageCreate

New message in a channel.

```javascript
connection.on("MessageCreate", (message) => {
  console.log("New message:", message);
});
```

---

#### MessageUpdate

Message was edited.

```javascript
connection.on("MessageUpdate", (message) => {
  console.log("Message edited:", message);
});
```

---

#### MessageDelete

Message was deleted.

```javascript
connection.on("MessageDelete", (data) => {
  console.log("Message deleted:", data.messageId);
});
```

---

#### PresenceUpdate

User's online status changed.

```javascript
connection.on("PresenceUpdate", (data) => {
  console.log("User presence:", data);
  // data = { userId, status, updatedAt }
});
```

---

#### VoiceStateUpdate

User joined/left voice or changed state.

```javascript
connection.on("VoiceStateUpdate", (voiceState) => {
  console.log("Voice state:", voiceState);
});
```

---

### Events to Send

#### SendMessage

Send a message via WebSocket.

```javascript
await connection.invoke("SendMessage", channelId, content);
```

---

#### JoinVoiceChannel

Join a voice channel.

```javascript
await connection.invoke("JoinVoiceChannel", channelId);
```

---

#### LeaveVoiceChannel

Leave voice channel.

```javascript
await connection.invoke("LeaveVoiceChannel", channelId);
```

---

#### SendWebRTCOffer

Send WebRTC offer for voice.

```javascript
await connection.invoke("SendWebRTCOffer", targetUserId, channelId, offer);
```

---

#### SendWebRTCAnswer

Send WebRTC answer.

```javascript
await connection.invoke("SendWebRTCAnswer", targetUserId, channelId, answer);
```

---

#### SendICECandidate

Send ICE candidate for WebRTC.

```javascript
await connection.invoke("SendICECandidate", targetUserId, channelId, candidate);
```

---

## Permission Flags

Permission system with 41 granular flags (Discord-compatible).

### Common Permissions

| Permission | Value | Description |
|------------|-------|-------------|
| `CreateInstantInvite` | 1 << 0 | Create invites |
| `KickMembers` | 1 << 1 | Kick members |
| `BanMembers` | 1 << 2 | Ban members |
| `Administrator` | 1 << 3 | All permissions |
| `ManageChannels` | 1 << 4 | Manage channels |
| `ManageGuild` | 1 << 5 | Manage server |
| `AddReactions` | 1 << 6 | Add reactions |
| `ViewAuditLog` | 1 << 7 | View audit log |
| `ViewChannel` | 1 << 10 | View channels |
| `SendMessages` | 1 << 11 | Send messages |
| `ManageMessages` | 1 << 13 | Delete/pin messages |
| `EmbedLinks` | 1 << 14 | Embed links |
| `AttachFiles` | 1 << 15 | Upload files |
| `ReadMessageHistory` | 1 << 16 | Read history |
| `MentionEveryone` | 1 << 17 | @everyone |
| `Connect` | 1 << 20 | Join voice |
| `Speak` | 1 << 21 | Speak in voice |
| `MuteMembers` | 1 << 22 | Mute members |
| `DeafenMembers` | 1 << 23 | Deafen members |
| `ManageRoles` | 1 << 28 | Manage roles |

---

## Snowflake IDs

YurtCord uses Twitter Snowflake IDs (64-bit integers as strings).

**Format**: `"123456789012345678"`

**Structure**:
- 41 bits: Timestamp (milliseconds since epoch)
- 10 bits: Worker ID
- 12 bits: Sequence number

**Benefits**:
- Sortable by creation time
- Globally unique
- No collisions in distributed systems

---

## Best Practices

### 1. **Use Pagination**

Always use `limit`, `before`, and `after` parameters when fetching messages:

```http
GET /api/channels/{channelId}/messages?limit=50&before=123
```

### 2. **Handle Rate Limits**

Implement exponential backoff when receiving 429 responses.

### 3. **WebSocket Reconnection**

Implement automatic reconnection logic for WebSocket disconnections.

### 4. **Cache User Data**

Cache user profiles and guild data to reduce API calls.

### 5. **Use WebSocket for Real-time**

Prefer WebSocket gateway for real-time events instead of polling REST API.

### 6. **Validate Input**

Always validate input before sending to API to avoid 400 errors.

---

## Example Integration

### Complete Example: Send a Message

```javascript
// 1. Authenticate
const loginResponse = await fetch('http://localhost:5000/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'john@example.com',
    password: 'Password123!'
  })
});

const { token } = await loginResponse.json();

// 2. Send message via REST
const messageResponse = await fetch(`http://localhost:5000/api/channels/${channelId}/messages`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({
    content: 'Hello from the API!',
    tts: false
  })
});

const message = await messageResponse.json();
console.log('Message sent:', message);
```

---

## Support

- **Interactive API Docs**: http://localhost:5000/swagger
- **GitHub Issues**: https://github.com/The404Studios/YurtCord/issues
- **Documentation**: See other .md files in the repository

---

**Built with ❤️ by The404Studios**
**API Version**: 1.0.0
**Last Updated**: 2025-01-15
