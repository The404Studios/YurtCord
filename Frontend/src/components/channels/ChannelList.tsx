import { useAppDispatch } from '../../store/hooks';
import { setCurrentChannel } from '../../store/slices/channelsSlice';
import { fetchMessages } from '../../store/slices/messagesSlice';
import type { Guild } from '../../types';
import { ChannelType } from '../../types';

interface ChannelListProps {
  guild: Guild;
  currentChannelId: string | null;
}

const ChannelList = ({ guild, currentChannelId }: ChannelListProps) => {
  const dispatch = useAppDispatch();

  const handleSelectChannel = (channelId: string) => {
    dispatch(setCurrentChannel(channelId));
    dispatch(fetchMessages(channelId));
  };

  // Group channels by category
  const categories = guild.channels.filter(c => c.type === ChannelType.GuildCategory);
  const textChannels = guild.channels.filter(c => c.type === ChannelType.GuildText && !c.parentId);
  const voiceChannels = guild.channels.filter(c => c.type === ChannelType.GuildVoice && !c.parentId);

  const renderChannel = (channel: any) => {
    const isActive = channel.id === currentChannelId;
    const isText = channel.type === ChannelType.GuildText || channel.type === ChannelType.GuildAnnouncement;
    const isVoice = channel.type === ChannelType.GuildVoice;

    return (
      <button
        key={channel.id}
        onClick={() => handleSelectChannel(channel.id)}
        className={`group w-full px-2 py-1.5 rounded flex items-center space-x-2 transition-all ${
          isActive
            ? 'bg-gray-700 text-white'
            : 'text-gray-400 hover:bg-gray-700/50 hover:text-gray-300'
        }`}
      >
        {isText && (
          <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
            <path d="M5.88657 21C5.57547 21 5.3399 20.7189 5.39427 20.4126L6.00001 17H2.59511C2.28449 17 2.04905 16.7198 2.10259 16.4138L2.27759 15.4138C2.31946 15.1746 2.52722 15 2.77011 15H6.35001L7.41001 9H4.00511C3.69449 9 3.45905 8.71977 3.51259 8.41381L3.68759 7.41381C3.72946 7.17456 3.93722 7 4.18011 7H7.76001L8.39677 3.41262C8.43914 3.17391 8.64664 3 8.88907 3H9.87344C10.1845 3 10.4201 3.28107 10.3657 3.58738L9.76001 7H15.76L16.3968 3.41262C16.4391 3.17391 16.6466 3 16.8891 3H17.8734C18.1845 3 18.4201 3.28107 18.3657 3.58738L17.76 7H21.1649C21.4755 7 21.711 7.28023 21.6574 7.58619L21.4824 8.58619C21.4406 8.82544 21.2328 9 20.9899 9H17.41L16.35 15H19.7549C20.0655 15 20.301 15.2802 20.2474 15.5862L20.0724 16.5862C20.0306 16.8254 19.8228 17 19.5799 17H16L15.3632 20.5874C15.3209 20.8261 15.1134 21 14.8709 21H13.8866C13.5755 21 13.3399 20.7189 13.3943 20.4126L14 17H8.00001L7.36325 20.5874C7.32088 20.8261 7.11337 21 6.87094 21H5.88657ZM9.41001 9L8.35001 15H14.35L15.41 9H9.41001Z" />
          </svg>
        )}
        {isVoice && (
          <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24">
            <path d="M12 3C10.34 3 9 4.34 9 6V10C9 11.66 10.34 13 12 13C13.66 13 15 11.66 15 10V6C15 4.34 13.66 3 12 3ZM11 15.92V18H8C7.45 18 7 18.45 7 19C7 19.55 7.45 20 8 20H16C16.55 20 17 19.55 17 19C17 18.45 16.55 18 16 18H13V15.92C15.84 15.48 18 13.03 18 10V9C18 8.45 17.55 8 17 8C16.45 8 16 8.45 16 9V10C16 12.21 14.21 14 12 14C9.79 14 8 12.21 8 10V9C8 8.45 7.55 8 7 8C6.45 8 6 8.45 6 9V10C6 13.03 8.16 15.48 11 15.92Z" />
          </svg>
        )}
        <span className="flex-1 text-left font-medium truncate">{channel.name}</span>
        {channel.nsfw && (
          <span className="text-xs bg-red-500/20 text-red-400 px-1.5 rounded">NSFW</span>
        )}
      </button>
    );
  };

  return (
    <div className="w-60 bg-gray-800 flex flex-col">
      {/* Server Header */}
      <div className="h-12 px-4 flex items-center border-b border-gray-900 shadow-md">
        <h2 className="font-bold text-white truncate">{guild.name}</h2>
        <button className="ml-auto text-gray-400 hover:text-white transition-colors">
          <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
            <path d="M6 10a2 2 0 11-4 0 2 2 0 014 0zM12 10a2 2 0 11-4 0 2 2 0 014 0zM16 12a2 2 0 100-4 2 2 0 000 4z" />
          </svg>
        </button>
      </div>

      {/* Channels List */}
      <div className="flex-1 overflow-y-auto px-2 py-2 space-y-1 scrollbar-thin scrollbar-thumb-gray-900 scrollbar-track-transparent">
        {/* Text Channels */}
        {textChannels.length > 0 && (
          <div className="mb-4">
            <div className="flex items-center px-0.5 mb-1">
              <svg className="w-3 h-3 text-gray-400 mr-1" fill="currentColor" viewBox="0 0 24 24">
                <path d="M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z" />
              </svg>
              <span className="text-xs font-semibold text-gray-400 uppercase tracking-wide">
                Text Channels
              </span>
            </div>
            {textChannels.map(renderChannel)}
          </div>
        )}

        {/* Voice Channels */}
        {voiceChannels.length > 0 && (
          <div>
            <div className="flex items-center px-0.5 mb-1">
              <svg className="w-3 h-3 text-gray-400 mr-1" fill="currentColor" viewBox="0 0 24 24">
                <path d="M7.41 8.59L12 13.17l4.59-4.58L18 10l-6 6-6-6 1.41-1.41z" />
              </svg>
              <span className="text-xs font-semibold text-gray-400 uppercase tracking-wide">
                Voice Channels
              </span>
            </div>
            {voiceChannels.map(renderChannel)}
          </div>
        )}
      </div>

      {/* User Panel */}
      <div className="h-14 bg-gray-900/50 px-2 flex items-center space-x-2">
        <div className="relative">
          <div className="w-8 h-8 bg-indigo-600 rounded-full flex items-center justify-center">
            <span className="text-sm font-semibold text-white">
              {guild.name.charAt(0)}
            </span>
          </div>
          <div className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-gray-900 rounded-full"></div>
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-white truncate">Username</p>
          <p className="text-xs text-gray-400 truncate">#0001</p>
        </div>
        <button className="text-gray-400 hover:text-white transition-colors">
          <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M11.49 3.17c-.38-1.56-2.6-1.56-2.98 0a1.532 1.532 0 01-2.286.948c-1.372-.836-2.942.734-2.106 2.106.54.886.061 2.042-.947 2.287-1.561.379-1.561 2.6 0 2.978a1.532 1.532 0 01.947 2.287c-.836 1.372.734 2.942 2.106 2.106a1.532 1.532 0 012.287.947c.379 1.561 2.6 1.561 2.978 0a1.533 1.533 0 012.287-.947c1.372.836 2.942-.734 2.106-2.106a1.533 1.533 0 01.947-2.287c1.561-.379 1.561-2.6 0-2.978a1.532 1.532 0 01-.947-2.287c.836-1.372-.734-2.942-2.106-2.106a1.532 1.532 0 01-2.287-.947zM10 13a3 3 0 100-6 3 3 0 000 6z" clipRule="evenodd" />
          </svg>
        </button>
      </div>
    </div>
  );
};

export default ChannelList;
