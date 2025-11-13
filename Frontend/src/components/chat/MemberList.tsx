import type { Guild } from '../../types';
import { UserStatus } from '../../types';

interface MemberListProps {
  guild: Guild;
}

const MemberList = ({ guild }: MemberListProps) => {
  // Group members by online status
  const onlineMembers = guild.members.filter(m => m.user.status === UserStatus.Online);
  const offlineMembers = guild.members.filter(m => m.user.status === UserStatus.Offline);

  const getStatusColor = (status: UserStatus) => {
    switch (status) {
      case UserStatus.Online:
        return 'bg-green-500';
      case UserStatus.Idle:
        return 'bg-yellow-500';
      case UserStatus.DoNotDisturb:
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  };

  const renderMember = (member: any) => (
    <button
      key={member.userId}
      className="w-full px-2 py-1.5 rounded flex items-center space-x-3 hover:bg-gray-700/50 transition-colors group"
    >
      <div className="relative flex-shrink-0">
        <div className="w-8 h-8 bg-indigo-600 rounded-full flex items-center justify-center">
          <span className="text-sm font-medium text-white">
            {member.user.username.charAt(0).toUpperCase()}
          </span>
        </div>
        <div className={`absolute bottom-0 right-0 w-3 h-3 ${getStatusColor(member.user.status)} border-2 border-gray-800 rounded-full`} />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-gray-300 group-hover:text-white truncate transition-colors">
          {member.nickname || member.user.username}
        </p>
        {member.user.customStatus && (
          <p className="text-xs text-gray-500 truncate">{member.user.customStatus}</p>
        )}
      </div>
    </button>
  );

  return (
    <div className="w-60 bg-gray-800 flex flex-col overflow-hidden">
      <div className="flex-1 overflow-y-auto px-2 py-2 space-y-4 scrollbar-thin scrollbar-thumb-gray-900 scrollbar-track-transparent">
        {/* Online Members */}
        <div>
          <div className="px-2 mb-2">
            <span className="text-xs font-semibold text-gray-400 uppercase tracking-wide">
              Online — {onlineMembers.length}
            </span>
          </div>
          <div className="space-y-0.5">
            {onlineMembers.map(renderMember)}
          </div>
        </div>

        {/* Offline Members */}
        {offlineMembers.length > 0 && (
          <div>
            <div className="px-2 mb-2">
              <span className="text-xs font-semibold text-gray-400 uppercase tracking-wide">
                Offline — {offlineMembers.length}
              </span>
            </div>
            <div className="space-y-0.5">
              {offlineMembers.map(renderMember)}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default MemberList;
