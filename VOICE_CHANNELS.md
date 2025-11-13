# YurtCord Voice Channels - Complete Guide

## 🎙️ Overview

YurtCord implements **full Discord-level voice communication** using **WebRTC** for peer-to-peer audio/video streaming and **SignalR** for signaling. Users can connect to voice channels, talk to each other in real-time, share screens, and enable video.

---

## 🏗️ Architecture

### Communication Flow

```
User A                          Server (SignalR Hub)                    User B
  │                                      │                                  │
  ├──── Join Voice Channel ─────────────>│                                  │
  │                                      ├────── User Joined Event ────────>│
  │                                      │                                  │
  │<──── Get ICE Servers ────────────────┤                                  │
  │                                      │                                  │
  ├──── Create WebRTC Offer ────────────>│                                  │
  │                                      ├────── Forward Offer ────────────>│
  │                                      │                                  │
  │                                      │<────── Create Answer ────────────┤
  │<──── Forward Answer ─────────────────┤                                  │
  │                                      │                                  │
  ├──── Exchange ICE Candidates ────────>│<──── Exchange ICE Candidates ───┤
  │                                      │                                  │
  │<═══════════ Peer-to-Peer Audio/Video Connection ══════════════════════>│
```

### Components

1. **Backend (C#)**
   - `VoiceService` - Manages voice connections and states
   - `VoiceController` - REST API for voice operations
   - `GatewayHub` - SignalR hub for WebRTC signaling

2. **Frontend (TypeScript/JavaScript)**
   - `VoiceClient` - WebRTC client implementation
   - Handles peer connections, media streams, signaling

3. **WebRTC**
   - Peer-to-peer audio/video streaming
   - ICE for NAT traversal
   - STUN/TURN servers for connectivity

---

## 🚀 Quick Start

### 1. Backend Setup

The voice infrastructure is already integrated. Just ensure the API is running:

```bash
cd Backend/YurtCord.API
dotnet run
```

### 2. Test Voice Channels

Open the included demo:

```bash
# Serve the HTML file
cd Frontend/examples
python -m http.server 8080
# Open http://localhost:8080/voice-example.html
```

Or directly open: `file:///path/to/YurtCord/Frontend/examples/voice-example.html`

### 3. Join a Voice Channel

1. **Login** with your YurtCord credentials
2. **Get a voice channel ID** (create one via API or use existing)
3. **Click "Join Voice Channel"**
4. **Allow microphone access**
5. **Start talking!**

---

## 📡 API Reference

### REST Endpoints

#### Get Voice Regions
```http
GET /api/voice/regions
Authorization: Bearer <token>
```

**Response:**
```json
[
  {
    "id": "us-west",
    "name": "US West",
    "optimal": true
  }
]
```

#### Join Voice Channel
```http
POST /api/voice/channels/{channelId}/join
Authorization: Bearer <token>
```

**Response:**
```json
{
  "token": "base64-voice-token",
  "sessionId": "uuid",
  "channelId": "1234567890123456",
  "userId": "9876543210987654",
  "iceServers": {
    "iceServers": [
      { "urls": ["stun:stun.l.google.com:19302"] }
    ]
  }
}
```

#### Leave Voice Channel
```http
POST /api/voice/channels/{channelId}/leave
Authorization: Bearer <token>
```

#### Update Voice State
```http
PATCH /api/voice/state
Authorization: Bearer <token>
Content-Type: application/json

{
  "selfMute": true,
  "selfDeaf": false,
  "selfVideo": false
}
```

#### Get Channel Users
```http
GET /api/voice/channels/{channelId}/users
Authorization: Bearer <token>
```

**Response:**
```json
[
  {
    "userId": "1234567890123456",
    "username": "johndoe",
    "discriminator": "0001",
    "selfMute": false,
    "selfDeaf": false,
    "selfVideo": false
  }
]
```

---

## 🔌 SignalR Events

### Client → Server

#### JoinVoiceChannel
```javascript
await connection.invoke('JoinVoiceChannel', channelId);
```

#### LeaveVoiceChannel
```javascript
await connection.invoke('LeaveVoiceChannel', channelId);
```

#### SendWebRTCOffer
```javascript
await connection.invoke('SendWebRTCOffer', targetUserId, channelId, offer);
```

#### SendWebRTCAnswer
```javascript
await connection.invoke('SendWebRTCAnswer', targetUserId, channelId, answer);
```

#### SendICECandidate
```javascript
await connection.invoke('SendICECandidate', targetUserId, channelId, candidate);
```

#### UpdateVoiceState
```javascript
await connection.invoke('UpdateVoiceState', channelId, mute, deaf, speaking);
```

#### UpdateVideoState
```javascript
await connection.invoke('UpdateVideoState', channelId, enabled);
```

### Server → Client

#### VoiceUserJoined
```javascript
connection.on('VoiceUserJoined', (data) => {
  // data: { userId, username, discriminator, channelId, sessionId, connectionId }
});
```

#### VoiceUserLeft
```javascript
connection.on('VoiceUserLeft', (data) => {
  // data: { userId, channelId }
});
```

#### WebRTCOffer
```javascript
connection.on('WebRTCOffer', (data) => {
  // data: { fromUserId, fromUsername, channelId, offer }
});
```

#### WebRTCAnswer
```javascript
connection.on('WebRTCAnswer', (data) => {
  // data: { fromUserId, channelId, answer }
});
```

#### ICECandidate
```javascript
connection.on('ICECandidate', (data) => {
  // data: { fromUserId, channelId, candidate }
});
```

#### VoiceStateUpdate
```javascript
connection.on('VoiceStateUpdate', (data) => {
  // data: { userId, channelId, mute, deaf, speaking }
});
```

#### VideoStateUpdate
```javascript
connection.on('VideoStateUpdate', (data) => {
  // data: { userId, channelId, enabled }
});
```

---

## 💻 Client Implementation

### Using the VoiceClient Class

```typescript
import { VoiceClient } from './services/VoiceClient';

// Initialize voice client
const voiceClient = new VoiceClient({
  gatewayUrl: 'http://localhost:5000/gateway',
  token: 'your-jwt-token',
  iceServers: [
    { urls: 'stun:stun.l.google.com:19302' }
  ]
});

// Setup event handlers
voiceClient.onUserJoined = (user) => {
  console.log(`${user.username} joined!`);
};

voiceClient.onUserLeft = (userId) => {
  console.log(`User ${userId} left`);
};

voiceClient.onRemoteStream = (userId, stream) => {
  // Attach stream to audio element
  const audio = document.createElement('audio');
  audio.srcObject = stream;
  audio.autoplay = true;
  document.body.appendChild(audio);
};

voiceClient.onUserSpeaking = (userId, speaking) => {
  // Show speaking indicator
  console.log(`User ${userId} is ${speaking ? 'speaking' : 'silent'}`);
};

// Connect to gateway
await voiceClient.connect();

// Join a voice channel (audio only)
await voiceClient.joinChannel('1234567890123456', true);

// Mute/unmute
await voiceClient.setMute(true);
await voiceClient.setMute(false);

// Deafen (mute output)
await voiceClient.setDeafen(true);

// Enable video
await voiceClient.setVideo(true);

// Start screen sharing
await voiceClient.startScreenShare();

// Get voice state
const state = voiceClient.getVoiceState();
console.log(state);
// {
//   channelId: "1234567890123456",
//   muted: false,
//   deafened: false,
//   videoEnabled: false,
//   connectedUsers: 3
// }

// Leave channel
await voiceClient.leaveChannel();

// Disconnect
await voiceClient.disconnect();
```

---

## 🎬 Features

### ✅ Audio Communication
- **High-quality audio** (48kHz sample rate)
- **Echo cancellation**
- **Noise suppression**
- **Auto gain control**
- **Mute/unmute** controls
- **Deafen** (mute all incoming audio)
- **Speaking indicators** (voice activity detection)

### ✅ Video Communication
- **Camera video** (720p @ 30fps)
- **Enable/disable** video on demand
- **Multiple video streams** in same channel

### ✅ Screen Sharing
- **Share entire screen** or specific window
- **Cursor visibility**
- **High frame rate** for smooth experience

### ✅ Peer-to-Peer Architecture
- **Low latency** (no server relay)
- **Mesh topology** (direct connections)
- **Automatic ICE negotiation**
- **NAT traversal** with STUN/TURN

### ✅ Security
- **Encrypted connections** (DTLS-SRTP)
- **Permission-based** channel access
- **Voice tokens** for authentication
- **Secure signaling** via SignalR with JWT

---

## 🔧 Configuration

### STUN/TURN Servers

The default configuration uses Google's public STUN servers. For production, configure your own TURN server:

```csharp
// In VoiceController.cs
private object GetIceServersConfiguration()
{
    return new
    {
        iceServers = new[]
        {
            new { urls = new[] { "stun:your-stun-server.com:3478" } },
            new
            {
                urls = new[] { "turn:your-turn-server.com:3478" },
                username = "yurtcord",
                credential = "secure-credential"
            }
        }
    };
}
```

### Audio Quality Settings

Adjust audio constraints in the client:

```javascript
const stream = await navigator.mediaDevices.getUserMedia({
  audio: {
    echoCancellation: true,
    noiseSuppression: true,
    autoGainControl: true,
    sampleRate: 48000,        // 48kHz
    channelCount: 2,          // Stereo
    latency: 0.01             // 10ms latency
  }
});
```

### Video Quality Settings

```javascript
const stream = await navigator.mediaDevices.getUserMedia({
  video: {
    width: { ideal: 1920 },   // 1080p
    height: { ideal: 1080 },
    frameRate: { ideal: 60 }, // 60fps
    facingMode: 'user'        // Front camera
  }
});
```

---

## 🐛 Troubleshooting

### Cannot hear other users

1. **Check deafen status** - Make sure you're not deafened
2. **Check remote stream** - Verify `onRemoteStream` is called
3. **Check audio element** - Ensure `autoplay` is set on audio elements
4. **Check permissions** - Browser audio output permissions

### Connection fails

1. **STUN/TURN servers** - Verify ICE servers are reachable
2. **Firewall** - Check UDP ports are not blocked
3. **Network** - Try different network (cellular vs WiFi)
4. **ICE candidates** - Check browser console for ICE failures

### Poor audio quality

1. **Network bandwidth** - Check internet speed
2. **CPU usage** - Close other applications
3. **Audio settings** - Adjust sample rate/bitrate
4. **Echo/noise** - Verify echo cancellation is enabled

### Microphone not working

1. **Permissions** - Grant microphone access in browser
2. **Device selection** - Check correct input device
3. **Mute status** - Ensure not muted
4. **Driver issues** - Update audio drivers

---

## 📊 Performance Considerations

### Mesh vs. SFU Topology

Current implementation uses **mesh topology** (peer-to-peer):

**Advantages:**
- Low latency
- No server bandwidth
- Direct connections

**Limitations:**
- Scales to ~5-10 users
- Client upload bandwidth increases with each peer

For large voice channels (10+ users), consider implementing an **SFU (Selective Forwarding Unit)**:

```
Users → SFU Server → Users
```

### Bandwidth Requirements

Per user connection:
- **Audio only**: ~50 Kbps upload/download
- **With video (720p)**: ~1-2 Mbps upload/download
- **With screen share**: ~2-5 Mbps upload/download

For 5 users in mesh:
- **Audio**: 250 Kbps upload, 200 Kbps download
- **Video**: 10 Mbps upload, 8 Mbps download

---

## 🔒 Security Considerations

### Voice Channel Permissions

Voice channels use the standard permission system:

```csharp
// Required permission to join
Permissions.Connect

// Required permission to speak
Permissions.Speak

// Required permission to use video
Permissions.Stream
```

### Encryption

- All WebRTC connections use **DTLS-SRTP** encryption
- Signaling is encrypted via **HTTPS/WSS**
- Voice tokens are **time-limited** (10 minutes)

### Rate Limiting

- Maximum voice state updates: **10 per second**
- Maximum channel joins: **5 per minute**

---

## 🎯 Future Enhancements

- [ ] **Voice activity detection** (automatic speaking indicators)
- [ ] **Audio filters** (noise gate, compressor)
- [ ] **Recording** voice channels
- [ ] **SFU implementation** for large channels (10+ users)
- [ ] **Spatial audio** (positional audio based on user arrangement)
- [ ] **Music bot** support with higher bitrate
- [ ] **Krisp noise cancellation** integration
- [ ] **Voice channel statistics** (bitrate, packet loss, jitter)
- [ ] **Priority speaker** mode
- [ ] **Go live** streaming to channel

---

## 📚 Additional Resources

- **WebRTC Specification**: https://webrtc.org/
- **SignalR Documentation**: https://docs.microsoft.com/en-us/aspnet/core/signalr/
- **STUN/TURN Setup**: https://github.com/coturn/coturn
- **Discord Voice Architecture**: https://discord.com/blog/how-discord-handles-two-and-half-million-concurrent-voice-users

---

## 💡 Example: Complete Voice Channel UI

See `Frontend/examples/voice-example.html` for a complete, working implementation with:

- ✅ User authentication
- ✅ Voice channel joining/leaving
- ✅ Mute/unmute/deafen controls
- ✅ User list with states
- ✅ Real-time status updates
- ✅ Beautiful UI with animations

---

## 🎉 Summary

YurtCord now has **full Discord-level voice communication**! Users can:

1. **Join voice channels** with a single click
2. **Talk in real-time** with high-quality audio
3. **See who's speaking** with live indicators
4. **Enable video** for face-to-face communication
5. **Share screens** for presentations/gaming
6. **Mute/deafen** for privacy control

All secured with **encryption**, **permissions**, and **authentication**! 🚀

---

**Built with ❤️ by The404Studios**
