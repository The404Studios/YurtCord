# SignalR Real-Time Integration

This document explains how SignalR is integrated into the YurtCord frontend for real-time messaging and events.

## Overview

SignalR provides real-time, bidirectional communication between the frontend and backend. Messages, reactions, typing indicators, and presence updates all happen instantly without polling or page refreshes.

## Architecture

### 1. SignalR Service (`src/services/signalr.ts`)

The `SignalRService` class is a singleton that manages the WebSocket connection to the backend.

**Key Features:**
- Automatic reconnection with exponential backoff (0s, 2s, 10s, 30s, 60s)
- Token-based authentication
- Event listener registration
- Channel join/leave management
- Connection state tracking

**Connection URL:**
```typescript
`${API_URL}/hubs/chat` // Default: http://localhost:5000/hubs/chat
```

### 2. React Hook (`src/hooks/useSignalR.ts`)

The `useSignalR` hook integrates SignalR with Redux state management.

**What it does:**
- Connects to SignalR when user is authenticated
- Listens for real-time events from the server
- Dispatches Redux actions when events are received
- Cleans up connections on unmount

**Usage:**
```typescript
const HomePage = () => {
  useSignalR(); // Initialize SignalR connection
  // ...
};
```

### 3. Integration with Redux

Real-time events automatically update Redux state:

| SignalR Event | Redux Action | Description |
|---------------|--------------|-------------|
| `MessageReceived` | `addMessage` | New message from another user |
| `MessageUpdated` | `updateMessage` | Message edited by user |
| `MessageDeleted` | `removeMessage` | Message deleted by user |
| `UserTyping` | (console log) | User is typing in channel |
| `UserPresenceChanged` | (console log) | User status changed |
| `ReactionAdded` | (console log) | Reaction added to message |
| `ReactionRemoved` | (console log) | Reaction removed from message |

## Real-Time Features

### 1. Instant Messaging

When a user sends a message:

**Flow:**
1. User types message and presses Enter
2. Frontend calls REST API `POST /api/channels/{channelId}/messages`
3. Backend saves message to database
4. Backend broadcasts `MessageReceived` event via SignalR
5. **All connected clients** receive the event
6. Redux state updates automatically
7. Message appears in chat for all users

**Code (ChatArea.tsx):**
```typescript
const handleSubmit = async (e: FormEvent) => {
  e.preventDefault();
  if (!content.trim()) return;

  // Sends via REST API, which triggers SignalR broadcast
  await dispatch(sendMessage({ channelId, content: content.trim() }));
  setContent('');
};
```

### 2. Channel Join/Leave

When a user switches channels:

**Flow:**
1. User clicks on a channel
2. Frontend leaves previous channel (via SignalR)
3. Frontend joins new channel (via SignalR)
4. Backend adds user to SignalR group for that channel
5. User now receives events only for that channel

**Code (ChatArea.tsx):**
```typescript
useEffect(() => {
  dispatch(fetchMessages(channelId));

  // Join SignalR channel
  if (signalRService.isConnected) {
    signalRService.joinChannel(channelId);
  }

  // Leave on cleanup
  return () => {
    if (signalRService.isConnected) {
      signalRService.leaveChannel(channelId);
    }
  };
}, [channelId, dispatch]);
```

### 3. Typing Indicators

When a user types:

**Flow:**
1. User types in message input
2. Frontend sends `SendTypingIndicator` via SignalR (debounced)
3. Backend broadcasts `UserTyping` event to channel
4. Other users see typing indicator (currently logged to console)

**Code (ChatArea.tsx):**
```typescript
const handleInputChange = (value: string) => {
  setContent(value);

  if (signalRService.isConnected && value.trim()) {
    signalRService.sendTypingIndicator(channelId);
  }
};
```

### 4. Connection Status

The `ConnectionStatus` component shows when SignalR is disconnected/reconnecting.

**States:**
- **Connected**: No indicator shown (everything working)
- **Connecting**: Yellow indicator with "Connecting..."
- **Reconnecting**: Yellow indicator with "Reconnecting..."
- **Disconnected**: Red indicator with "Disconnected"

## Backend Integration

### Required Backend Hub

The backend must implement a SignalR hub at `/hubs/chat` with these methods:

**Server Methods (called by frontend):**
```csharp
Task JoinChannel(string channelId)
Task LeaveChannel(string channelId)
Task SendMessage(string channelId, string content)
Task SendTypingIndicator(string channelId)
Task UpdatePresence(string status)
```

**Client Events (sent to frontend):**
```csharp
MessageReceived(Message message)
MessageUpdated(Message message)
MessageDeleted(string messageId)
UserTyping(UserTypingData data)
UserPresenceChanged(PresenceData data)
ReactionAdded(ReactionData data)
ReactionRemoved(ReactionData data)
```

### Authentication

SignalR uses JWT Bearer token authentication:

```typescript
.withUrl(`${API_URL}/hubs/chat`, {
  accessTokenFactory: () => token,
  // Token from Redux auth state
})
```

The backend validates the token and sets `Context.User` for authorization.

## Testing SignalR

### 1. Check Connection

Open browser console and look for:
```
✅ SignalR connected
Joined channel: {channelId}
```

### 2. Test Messaging

1. Open YurtCord in two browser windows
2. Login as different users (alice@example.com, bob@example.com)
3. Both join the same channel
4. Send message from one window
5. Message should appear **instantly** in both windows

### 3. Test Reconnection

1. Stop the backend server
2. Connection status indicator should appear (red "Disconnected")
3. Start the backend server
4. Connection should automatically reconnect (yellow "Reconnecting..." then disappears)

### 4. Check Network Tab

1. Open DevTools → Network tab
2. Filter by "WS" (WebSocket)
3. You should see: `ws://localhost:5000/hubs/chat`
4. Status should be "101 Switching Protocols" (WebSocket upgrade)

## Troubleshooting

### SignalR Not Connecting

**Symptoms:**
- "Disconnected" indicator always showing
- No real-time updates
- Console errors about SignalR

**Solutions:**
1. Check backend is running on port 5000
2. Verify `/hubs/chat` endpoint exists
3. Check JWT token is valid (not expired)
4. Look for CORS errors in console
5. Ensure WebSocket is enabled on backend

### Messages Not Appearing in Real-Time

**Symptoms:**
- Messages only appear after refresh
- Sent messages don't show for other users

**Solutions:**
1. Check SignalR is connected (no red indicator)
2. Verify user joined the channel
3. Check browser console for errors
4. Ensure backend broadcasts `MessageReceived` event
5. Verify Redux state is updating (Redux DevTools)

### Duplicate Messages

**Symptoms:**
- Same message appears twice
- Messages duplicate on send

**Solutions:**
- The `addMessage` reducer already has duplicate prevention:
  ```typescript
  const exists = state.messages[channelId].some(m => m.id === action.payload.id);
  if (!exists) {
    state.messages[channelId].push(action.payload);
  }
  ```
- Make sure message IDs are unique (backend generates snowflake IDs)

## Performance Considerations

### 1. Automatic Reconnection

SignalR automatically reconnects on connection loss with exponential backoff:
- 1st retry: 0 seconds
- 2nd retry: 2 seconds
- 3rd retry: 10 seconds
- 4th retry: 30 seconds
- 5th+ retries: 60 seconds

### 2. Transport Fallback

SignalR tries WebSocket first, then falls back to Long Polling if WebSocket is unavailable:

```typescript
transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
```

### 3. Typing Indicator Debouncing

Typing indicators are debounced to avoid spamming the server:
- Sent once when user starts typing
- Not sent again for 3 seconds
- Prevents excessive network traffic

## Future Enhancements

### Potential Improvements:

1. **Visual Typing Indicators**
   - Show "User is typing..." in chat
   - Display typing users below message input
   - Animate typing dots

2. **Presence System**
   - Show online/offline status in member list
   - Update status in real-time
   - Display "last seen" timestamps

3. **Reactions**
   - Add/remove reactions to messages
   - Real-time reaction updates
   - Animated reaction popups

4. **Voice Channel Presence**
   - Show who's in voice channels
   - Real-time join/leave updates
   - Speaking indicators

5. **Read Receipts**
   - Track which messages user has read
   - Show "read by" indicators
   - Sync read state across devices

6. **Optimistic Updates**
   - Show sent messages immediately (before server confirms)
   - Display pending state
   - Rollback on error

## Related Files

- `src/services/signalr.ts` - SignalR service singleton
- `src/hooks/useSignalR.ts` - React hook for SignalR
- `src/components/chat/ChatArea.tsx` - Channel join/leave integration
- `src/components/common/ConnectionStatus.tsx` - Connection status indicator
- `src/store/slices/messagesSlice.ts` - Message state management
- `src/pages/HomePage.tsx` - SignalR initialization

## References

- [SignalR JavaScript Client Docs](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client)
- [ASP.NET Core SignalR Hubs](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs)
- [SignalR Authentication](https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz)
