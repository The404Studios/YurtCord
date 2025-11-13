import { useEffect, useRef } from 'react';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import { signalRService } from '../services/signalr';
import { addMessage, updateMessage, removeMessage } from '../store/slices/messagesSlice';
import { updateUserPresence } from '../store/slices/presenceSlice';
import { UserStatus } from '../types';
import toast from 'react-hot-toast';

export const useSignalR = () => {
  const dispatch = useAppDispatch();
  const { token } = useAppSelector((state) => state.auth);
  const isConnectedRef = useRef(false);

  useEffect(() => {
    if (!token || isConnectedRef.current) return;

    const connectSignalR = async () => {
      try {
        await signalRService.connect(token);
        isConnectedRef.current = true;

        // Set up event listeners
        signalRService.onMessageReceived((message) => {
          console.log('📨 Message received:', message);
          dispatch(addMessage(message));
        });

        signalRService.onMessageUpdated((message) => {
          console.log('✏️ Message updated:', message);
          dispatch(updateMessage(message));
        });

        signalRService.onMessageDeleted((messageId) => {
          console.log('🗑️ Message deleted:', messageId);
          dispatch(removeMessage(messageId));
        });

        signalRService.onUserTyping(({ username, channelId }) => {
          // Could show typing indicator in UI
          console.log(`⌨️ ${username} is typing in channel ${channelId}`);
        });

        signalRService.onUserPresenceChanged(({ userId, status }) => {
          console.log(`👤 User ${userId} status changed to ${status}`);
          dispatch(updateUserPresence({
            userId,
            status: status as UserStatus,
            lastSeen: new Date().toISOString(),
          }));
        });

        signalRService.onReactionAdded(({ messageId, emoji, userId }) => {
          console.log(`👍 Reaction added: ${emoji} on message ${messageId} by ${userId}`);
          // Could update message reactions in Redux
        });

        signalRService.onReactionRemoved(({ messageId, emoji, userId }) => {
          console.log(`👎 Reaction removed: ${emoji} on message ${messageId} by ${userId}`);
          // Could update message reactions in Redux
        });

        toast.success('Connected to real-time chat');
      } catch (error) {
        console.error('Failed to connect to SignalR:', error);
        toast.error('Failed to connect to real-time chat');
        isConnectedRef.current = false;
      }
    };

    connectSignalR();

    // Cleanup on unmount
    return () => {
      if (isConnectedRef.current) {
        signalRService.removeAllListeners();
        signalRService.disconnect();
        isConnectedRef.current = false;
      }
    };
  }, [token, dispatch]);

  return {
    isConnected: signalRService.isConnected,
    joinChannel: signalRService.joinChannel.bind(signalRService),
    leaveChannel: signalRService.leaveChannel.bind(signalRService),
    sendMessage: signalRService.sendMessage.bind(signalRService),
    sendTypingIndicator: signalRService.sendTypingIndicator.bind(signalRService),
    updatePresence: signalRService.updatePresence.bind(signalRService),
  };
};
