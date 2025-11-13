import { useEffect, useRef, useState, FormEvent } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import { fetchMessages, sendMessage } from '../../store/slices/messagesSlice';
import { signalRService } from '../../services/signalr';
import type { Guild } from '../../types';
import MessageItem from './MessageItem';

interface ChatAreaProps {
  channelId: string;
  guild: Guild;
}

const ChatArea = ({ channelId, guild }: ChatAreaProps) => {
  const dispatch = useAppDispatch();
  const messages = useAppSelector((state) => state.messages.messages[channelId] || []);
  const [content, setContent] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  const channel = guild.channels.find(c => c.id === channelId);

  // Fetch messages and join SignalR channel when channelId changes
  useEffect(() => {
    dispatch(fetchMessages(channelId));

    // Join SignalR channel if connected
    if (signalRService.isConnected) {
      signalRService.joinChannel(channelId).catch(err => {
        console.error('Failed to join channel via SignalR:', err);
      });
    }

    // Leave channel on cleanup
    return () => {
      if (signalRService.isConnected) {
        signalRService.leaveChannel(channelId).catch(err => {
          console.error('Failed to leave channel via SignalR:', err);
        });
      }
    };
  }, [channelId, dispatch]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleInputChange = (value: string) => {
    setContent(value);

    // Send typing indicator
    if (signalRService.isConnected && value.trim()) {
      // Clear previous timeout
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }

      // Send typing indicator
      signalRService.sendTypingIndicator(channelId).catch(err => {
        console.error('Failed to send typing indicator:', err);
      });

      // Set timeout to stop showing typing after 3 seconds
      typingTimeoutRef.current = setTimeout(() => {
        typingTimeoutRef.current = null;
      }, 3000);
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!content.trim()) return;

    // Clear typing timeout
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
      typingTimeoutRef.current = null;
    }

    await dispatch(sendMessage({ channelId, content: content.trim() }));
    setContent('');
  };

  return (
    <div className="flex-1 flex flex-col bg-gray-700">
      {/* Channel Header */}
      <div className="h-12 px-4 flex items-center border-b border-gray-900 shadow-md bg-gray-800">
        <svg className="w-6 h-6 text-gray-400 mr-2" fill="currentColor" viewBox="0 0 24 24">
          <path d="M5.88657 21C5.57547 21 5.3399 20.7189 5.39427 20.4126L6.00001 17H2.59511C2.28449 17 2.04905 16.7198 2.10259 16.4138L2.27759 15.4138C2.31946 15.1746 2.52722 15 2.77011 15H6.35001L7.41001 9H4.00511C3.69449 9 3.45905 8.71977 3.51259 8.41381L3.68759 7.41381C3.72946 7.17456 3.93722 7 4.18011 7H7.76001L8.39677 3.41262C8.43914 3.17391 8.64664 3 8.88907 3H9.87344C10.1845 3 10.4201 3.28107 10.3657 3.58738L9.76001 7H15.76L16.3968 3.41262C16.4391 3.17391 16.6466 3 16.8891 3H17.8734C18.1845 3 18.4201 3.28107 18.3657 3.58738L17.76 7H21.1649C21.4755 7 21.711 7.28023 21.6574 7.58619L21.4824 8.58619C21.4406 8.82544 21.2328 9 20.9899 9H17.41L16.35 15H19.7549C20.0655 15 20.301 15.2802 20.2474 15.5862L20.0724 16.5862C20.0306 16.8254 19.8228 17 19.5799 17H16L15.3632 20.5874C15.3209 20.8261 15.1134 21 14.8709 21H13.8866C13.5755 21 13.3399 20.7189 13.3943 20.4126L14 17H8.00001L7.36325 20.5874C7.32088 20.8261 7.11337 21 6.87094 21H5.88657ZM9.41001 9L8.35001 15H14.35L15.41 9H9.41001Z" />
        </svg>
        <h3 className="font-semibold text-white">{channel?.name || 'Channel'}</h3>
        {channel?.topic && (
          <>
            <div className="w-px h-6 bg-gray-600 mx-2" />
            <p className="text-sm text-gray-400 truncate">{channel.topic}</p>
          </>
        )}
      </div>

      {/* Messages Area */}
      <div className="flex-1 overflow-y-auto px-4 py-4 space-y-4 scrollbar-thin scrollbar-thumb-gray-900 scrollbar-track-transparent">
        {messages.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-center">
            <div className="w-16 h-16 bg-gray-600 rounded-full flex items-center justify-center mb-4">
              <svg className="w-8 h-8 text-gray-400" fill="currentColor" viewBox="0 0 24 24">
                <path d="M5.88657 21C5.57547 21 5.3399 20.7189 5.39427 20.4126L6.00001 17H2.59511C2.28449 17 2.04905 16.7198 2.10259 16.4138L2.27759 15.4138C2.31946 15.1746 2.52722 15 2.77011 15H6.35001L7.41001 9H4.00511C3.69449 9 3.45905 8.71977 3.51259 8.41381L3.68759 7.41381C3.72946 7.17456 3.93722 7 4.18011 7H7.76001L8.39677 3.41262C8.43914 3.17391 8.64664 3 8.88907 3H9.87344C10.1845 3 10.4201 3.28107 10.3657 3.58738L9.76001 7H15.76L16.3968 3.41262C16.4391 3.17391 16.6466 3 16.8891 3H17.8734C18.1845 3 18.4201 3.28107 18.3657 3.58738L17.76 7H21.1649C21.4755 7 21.711 7.28023 21.6574 7.58619L21.4824 8.58619C21.4406 8.82544 21.2328 9 20.9899 9H17.41L16.35 15H19.7549C20.0655 15 20.301 15.2802 20.2474 15.5862L20.0724 16.5862C20.0306 16.8254 19.8228 17 19.5799 17H16L15.3632 20.5874C15.3209 20.8261 15.1134 21 14.8709 21H13.8866C13.5755 21 13.3399 20.7189 13.3943 20.4126L14 17H8.00001L7.36325 20.5874C7.32088 20.8261 7.11337 21 6.87094 21H5.88657ZM9.41001 9L8.35001 15H14.35L15.41 9H9.41001Z" />
              </svg>
            </div>
            <h3 className="text-xl font-bold text-white mb-2">
              Welcome to #{channel?.name}!
            </h3>
            <p className="text-gray-400">
              This is the start of the #{channel?.name} channel.
            </p>
          </div>
        ) : (
          messages.map((message) => (
            <MessageItem key={message.id} message={message} />
          ))
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Message Input */}
      <div className="p-4">
        <form onSubmit={handleSubmit}>
          <div className="bg-gray-600 rounded-lg px-4 py-3 flex items-center space-x-2">
            <button
              type="button"
              className="text-gray-400 hover:text-white transition-colors"
            >
              <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
              </svg>
            </button>
            <input
              type="text"
              value={content}
              onChange={(e) => handleInputChange(e.target.value)}
              className="flex-1 bg-transparent text-white placeholder-gray-400 focus:outline-none"
              placeholder={`Message #${channel?.name || 'channel'}`}
            />
            <button
              type="button"
              className="text-gray-400 hover:text-white transition-colors"
            >
              <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
                <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zM8.5 9c.69 0 1.25.56 1.25 1.25S9.19 11.5 8.5 11.5s-1.25-.56-1.25-1.25S7.81 9 8.5 9zm7 0c.69 0 1.25.56 1.25 1.25s-.56 1.25-1.25 1.25-1.25-.56-1.25-1.25S14.81 9 15.5 9zM12 17.5c-2.33 0-4.31-1.46-5.11-3.5h10.22c-.8 2.04-2.78 3.5-5.11 3.5z" />
              </svg>
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default ChatArea;
