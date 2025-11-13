import * as signalR from '@microsoft/signalr';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private token: string | null = null;

  async connect(token: string): Promise<void> {
    this.token = token;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/chat`, {
        accessTokenFactory: () => token,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s, then 60s
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          if (retryContext.previousRetryCount === 3) return 30000;
          return 60000;
        },
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    try {
      await this.connection.start();
      console.log('✅ SignalR connected');
    } catch (err) {
      console.error('❌ SignalR connection error:', err);
      throw err;
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.token = null;
      console.log('SignalR disconnected');
    }
  }

  // Event Listeners
  onMessageReceived(callback: (message: any) => void): void {
    this.connection?.on('MessageReceived', callback);
  }

  onMessageUpdated(callback: (message: any) => void): void {
    this.connection?.on('MessageUpdated', callback);
  }

  onMessageDeleted(callback: (messageId: string) => void): void {
    this.connection?.on('MessageDeleted', callback);
  }

  onUserTyping(callback: (data: { userId: string; channelId: string; username: string }) => void): void {
    this.connection?.on('UserTyping', callback);
  }

  onUserPresenceChanged(callback: (data: { userId: string; status: string }) => void): void {
    this.connection?.on('UserPresenceChanged', callback);
  }

  onReactionAdded(callback: (data: { messageId: string; emoji: string; userId: string }) => void): void {
    this.connection?.on('ReactionAdded', callback);
  }

  onReactionRemoved(callback: (data: { messageId: string; emoji: string; userId: string }) => void): void {
    this.connection?.on('ReactionRemoved', callback);
  }

  // Actions
  async joinChannel(channelId: string): Promise<void> {
    if (!this.connection) throw new Error('SignalR not connected');
    await this.connection.invoke('JoinChannel', channelId);
    console.log(`Joined channel: ${channelId}`);
  }

  async leaveChannel(channelId: string): Promise<void> {
    if (!this.connection) throw new Error('SignalR not connected');
    await this.connection.invoke('LeaveChannel', channelId);
    console.log(`Left channel: ${channelId}`);
  }

  async sendMessage(channelId: string, content: string): Promise<void> {
    if (!this.connection) throw new Error('SignalR not connected');
    await this.connection.invoke('SendMessage', channelId, content);
  }

  async sendTypingIndicator(channelId: string): Promise<void> {
    if (!this.connection) throw new Error('SignalR not connected');
    await this.connection.invoke('SendTypingIndicator', channelId);
  }

  async updatePresence(status: string): Promise<void> {
    if (!this.connection) throw new Error('SignalR not connected');
    await this.connection.invoke('UpdatePresence', status);
  }

  // Connection State
  get isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  get connectionState(): signalR.HubConnectionState | null {
    return this.connection?.state ?? null;
  }

  // Remove all listeners (cleanup)
  removeAllListeners(): void {
    this.connection?.off('MessageReceived');
    this.connection?.off('MessageUpdated');
    this.connection?.off('MessageDeleted');
    this.connection?.off('UserTyping');
    this.connection?.off('UserPresenceChanged');
    this.connection?.off('ReactionAdded');
    this.connection?.off('ReactionRemoved');
  }
}

// Singleton instance
export const signalRService = new SignalRService();
