import { useAppDispatch } from '../../store/hooks';
import { setCurrentGuild } from '../../store/slices/guildsSlice';
import { clearCurrentChannel } from '../../store/slices/channelsSlice';
import type { Guild } from '../../types';

interface ServerListProps {
  guilds: Guild[];
  currentGuildId?: string;
}

const ServerList = ({ guilds, currentGuildId }: ServerListProps) => {
  const dispatch = useAppDispatch();

  const handleSelectGuild = (guildId: string) => {
    dispatch(setCurrentGuild(guildId));
    dispatch(clearCurrentChannel());
  };

  return (
    <div className="flex flex-col items-center w-[72px] bg-gray-950 py-3 space-y-2 overflow-y-auto scrollbar-hide">
      {/* Home Button */}
      <button
        className="group relative w-12 h-12 bg-gray-800 hover:bg-indigo-600 rounded-full flex items-center justify-center transition-all duration-200 hover:rounded-2xl"
      >
        <svg className="w-6 h-6 text-gray-400 group-hover:text-white transition-colors" fill="currentColor" viewBox="0 0 20 20">
          <path d="M10.707 2.293a1 1 0 00-1.414 0l-7 7a1 1 0 001.414 1.414L4 10.414V17a1 1 0 001 1h2a1 1 0 001-1v-2a1 1 0 011-1h2a1 1 0 011 1v2a1 1 0 001 1h2a1 1 0 001-1v-6.586l.293.293a1 1 0 001.414-1.414l-7-7z" />
        </svg>
        <span className="absolute left-full ml-2 px-2 py-1 bg-gray-900 text-white text-sm rounded whitespace-nowrap opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity">
          Home
        </span>
      </button>

      {/* Separator */}
      <div className="w-8 h-[2px] bg-gray-800 rounded-full" />

      {/* Server Icons */}
      {guilds.map((guild) => {
        const isActive = guild.id === currentGuildId;
        return (
          <button
            key={guild.id}
            onClick={() => handleSelectGuild(guild.id)}
            className={`group relative w-12 h-12 ${
              isActive
                ? 'bg-indigo-600 rounded-2xl'
                : 'bg-gray-800 hover:bg-indigo-600 rounded-full hover:rounded-2xl'
            } flex items-center justify-center transition-all duration-200 overflow-hidden`}
          >
            {guild.icon ? (
              <img
                src={guild.icon}
                alt={guild.name}
                className="w-full h-full object-cover"
              />
            ) : (
              <span className={`text-lg font-semibold ${
                isActive ? 'text-white' : 'text-gray-400 group-hover:text-white'
              } transition-colors`}>
                {guild.name.charAt(0).toUpperCase()}
              </span>
            )}

            {/* Tooltip */}
            <span className="absolute left-full ml-2 px-2 py-1 bg-gray-900 text-white text-sm rounded whitespace-nowrap opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity z-50">
              {guild.name}
            </span>

            {/* Active Indicator */}
            {isActive && (
              <div className="absolute -left-2 top-1/2 -translate-y-1/2 w-1 h-8 bg-white rounded-r-full animate-slide-in-left" />
            )}
          </button>
        );
      })}

      {/* Add Server Button */}
      <button className="group relative w-12 h-12 bg-gray-800 hover:bg-green-600 rounded-full flex items-center justify-center transition-all duration-200 hover:rounded-2xl">
        <svg className="w-6 h-6 text-green-500 group-hover:text-white transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
        </svg>
        <span className="absolute left-full ml-2 px-2 py-1 bg-gray-900 text-white text-sm rounded whitespace-nowrap opacity-0 group-hover:opacity-100 pointer-events-none transition-opacity">
          Add a Server
        </span>
      </button>
    </div>
  );
};

export default ServerList;
