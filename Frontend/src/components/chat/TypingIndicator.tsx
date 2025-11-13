import { useEffect, useState } from 'react';

interface TypingUser {
  userId: string;
  username: string;
  timestamp: number;
}

interface TypingIndicatorProps {
  typingUsers: Map<string, TypingUser>;
  currentUserId: string;
}

const TypingIndicator = ({ typingUsers, currentUserId }: TypingIndicatorProps) => {
  const [displayUsers, setDisplayUsers] = useState<TypingUser[]>([]);

  useEffect(() => {
    // Filter out current user and expired typing indicators (>3 seconds old)
    const now = Date.now();
    const activeUsers = Array.from(typingUsers.values()).filter(
      (user) => user.userId !== currentUserId && now - user.timestamp < 3000
    );

    setDisplayUsers(activeUsers);
  }, [typingUsers, currentUserId]);

  if (displayUsers.length === 0) {
    return null;
  }

  const getTypingText = () => {
    if (displayUsers.length === 1) {
      return `${displayUsers[0].username} is typing`;
    } else if (displayUsers.length === 2) {
      return `${displayUsers[0].username} and ${displayUsers[1].username} are typing`;
    } else if (displayUsers.length === 3) {
      return `${displayUsers[0].username}, ${displayUsers[1].username}, and ${displayUsers[2].username} are typing`;
    } else {
      return `Several people are typing`;
    }
  };

  return (
    <div className="px-4 py-2 text-sm text-gray-400 animate-fade-in flex items-center space-x-1">
      <span>{getTypingText()}</span>
      <div className="flex space-x-1 ml-1">
        <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
        <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
        <div className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
      </div>
    </div>
  );
};

export default TypingIndicator;
