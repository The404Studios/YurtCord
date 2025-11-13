import { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../store/hooks';
import { fetchGuilds } from '../store/slices/guildsSlice';
import { useSignalR } from '../hooks/useSignalR';
import ServerList from '../components/servers/ServerList';
import ChannelList from '../components/channels/ChannelList';
import ChatArea from '../components/chat/ChatArea';
import MemberList from '../components/chat/MemberList';
import LoadingSpinner from '../components/common/LoadingSpinner';
import ConnectionStatus from '../components/common/ConnectionStatus';

const HomePage = () => {
  const dispatch = useAppDispatch();
  const { guilds, currentGuild, loading } = useAppSelector((state) => state.guilds);
  const { currentChannelId } = useAppSelector((state) => state.channels);

  // Initialize SignalR connection
  useSignalR();

  useEffect(() => {
    dispatch(fetchGuilds());
  }, [dispatch]);

  if (loading && guilds.length === 0) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-900">
        <LoadingSpinner size="large" />
      </div>
    );
  }

  return (
    <div className="flex h-screen bg-gray-900 overflow-hidden">
      {/* Server List */}
      <ServerList guilds={guilds} currentGuildId={currentGuild?.id} />

      {/* Channel List */}
      {currentGuild && (
        <ChannelList
          guild={currentGuild}
          currentChannelId={currentChannelId}
        />
      )}

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col min-w-0">
        {currentChannelId && currentGuild ? (
          <ChatArea
            channelId={currentChannelId}
            guild={currentGuild}
          />
        ) : (
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center">
              <h2 className="text-2xl font-bold text-gray-400 mb-2">
                Welcome to YurtCord!
              </h2>
              <p className="text-gray-500">
                Select a server and channel to start chatting
              </p>
            </div>
          </div>
        )}
      </div>

      {/* Member List */}
      {currentChannelId && currentGuild && (
        <MemberList guild={currentGuild} />
      )}

      {/* Connection Status Indicator */}
      <ConnectionStatus />
    </div>
  );
};

export default HomePage;
