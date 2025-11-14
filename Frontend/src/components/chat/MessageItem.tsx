import { useState, useRef, useEffect } from 'react';
import { useAppSelector } from '../../store/hooks';
import type { Message } from '../../types';
import axios from 'axios';
import toast from 'react-hot-toast';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

interface MessageItemProps {
  message: Message;
}

const MessageItem = ({ message }: MessageItemProps) => {
  const currentUser = useAppSelector((state) => state.auth.user);
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState(message.content);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const editInputRef = useRef<HTMLInputElement>(null);

  const isOwnMessage = currentUser?.id === message.authorId;

  const timestamp = new Date(message.timestamp).toLocaleString('en-US', {
    hour: '2-digit',
    minute: '2-digit',
  });

  const editedAt = message.editedTimestamp
    ? new Date(message.editedTimestamp).toLocaleString('en-US', {
        hour: '2-digit',
        minute: '2-digit',
      })
    : null;

  useEffect(() => {
    if (isEditing && editInputRef.current) {
      editInputRef.current.focus();
      editInputRef.current.select();
    }
  }, [isEditing]);

  const handleEdit = async () => {
    if (!editContent.trim() || editContent === message.content) {
      setIsEditing(false);
      setEditContent(message.content);
      return;
    }

    try {
      const token = localStorage.getItem('token');
      await axios.patch(
        `${API_URL}/api/messages/${message.id}`,
        { content: editContent.trim() },
        { headers: { Authorization: `Bearer ${token}` } }
      );
      setIsEditing(false);
      toast.success('Message edited');
    } catch (error: any) {
      console.error('Failed to edit message:', error);
      toast.error(error.response?.data?.message || 'Failed to edit message');
      setEditContent(message.content);
      setIsEditing(false);
    }
  };

  const handleDelete = async () => {
    try {
      const token = localStorage.getItem('token');
      await axios.delete(`${API_URL}/api/messages/${message.id}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      setShowDeleteConfirm(false);
      toast.success('Message deleted');
    } catch (error: any) {
      console.error('Failed to delete message:', error);
      toast.error(error.response?.data?.message || 'Failed to delete message');
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleEdit();
    } else if (e.key === 'Escape') {
      setIsEditing(false);
      setEditContent(message.content);
    }
  };

  return (
    <div className="group flex items-start space-x-4 hover:bg-gray-800/30 px-4 py-2 -mx-4 rounded animate-fade-in">
      {/* Avatar */}
      <div className="w-10 h-10 bg-indigo-600 rounded-full flex-shrink-0 flex items-center justify-center">
        <span className="text-sm font-semibold text-white">
          {message.author.username.charAt(0).toUpperCase()}
        </span>
      </div>

      {/* Message Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-baseline space-x-2">
          <span className="font-semibold text-white hover:underline cursor-pointer">
            {message.author.username}
          </span>
          <span className="text-xs text-gray-500">{timestamp}</span>
          {editedAt && !isEditing && (
            <span className="text-xs text-gray-500 italic" title={`Edited at ${editedAt}`}>
              (edited)
            </span>
          )}
        </div>

        {/* Edit Mode */}
        {isEditing ? (
          <div className="mt-1">
            <input
              ref={editInputRef}
              type="text"
              value={editContent}
              onChange={(e) => setEditContent(e.target.value)}
              onKeyDown={handleKeyDown}
              className="w-full bg-gray-700 text-white px-2 py-1 rounded focus:outline-none focus:ring-2 focus:ring-indigo-500"
              placeholder="Edit message..."
            />
            <div className="mt-1 text-xs text-gray-400">
              Press Enter to save • Escape to cancel
            </div>
          </div>
        ) : (
          <p className="text-gray-300 break-words">{message.content}</p>
        )}

        {/* Attachments */}
        {message.attachments.length > 0 && (
          <div className="mt-2 space-y-2">
            {message.attachments.map((attachment) => (
              <div key={attachment.id} className="max-w-md">
                {attachment.contentType?.startsWith('image/') ? (
                  <img
                    src={attachment.url}
                    alt={attachment.filename}
                    className="rounded-lg max-h-96 cursor-pointer hover:opacity-90 transition-opacity"
                  />
                ) : (
                  <a
                    href={attachment.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-center space-x-2 bg-gray-800 rounded p-3 hover:bg-gray-750 transition-colors"
                  >
                    <svg className="w-6 h-6 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M8 4a3 3 0 00-3 3v4a5 5 0 0010 0V7a1 1 0 112 0v4a7 7 0 11-14 0V7a5 5 0 0110 0v4a3 3 0 11-6 0V7a1 1 0 012 0v4a1 1 0 102 0V7a3 3 0 00-3-3z" clipRule="evenodd" />
                    </svg>
                    <div>
                      <p className="text-sm text-blue-400 hover:underline">{attachment.filename}</p>
                      <p className="text-xs text-gray-500">{(attachment.size / 1024).toFixed(1)} KB</p>
                    </div>
                  </a>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Reactions */}
        {message.reactions.length > 0 && (
          <div className="mt-2 flex flex-wrap gap-1">
            {message.reactions.map((reaction, index) => (
              <button
                key={index}
                className={`flex items-center space-x-1 px-2 py-1 rounded ${
                  reaction.me
                    ? 'bg-indigo-600/30 border border-indigo-500'
                    : 'bg-gray-800 border border-gray-700 hover:border-gray-600'
                } transition-colors`}
              >
                <span className="text-sm">{reaction.emojiName}</span>
                <span className="text-xs text-gray-400">{reaction.count}</span>
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Message Actions (visible on hover) */}
      {!isEditing && (
        <div className="opacity-0 group-hover:opacity-100 transition-opacity flex items-center space-x-1">
          {/* Add Reaction Button */}
          <button
            className="p-1.5 bg-gray-800 hover:bg-gray-750 rounded text-gray-400 hover:text-white transition-colors"
            title="Add reaction"
          >
            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
              <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
              <path d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" />
            </svg>
          </button>

          {/* Edit Button (only for own messages) */}
          {isOwnMessage && (
            <button
              onClick={() => setIsEditing(true)}
              className="p-1.5 bg-gray-800 hover:bg-gray-750 rounded text-gray-400 hover:text-white transition-colors"
              title="Edit message"
            >
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path d="M17.414 2.586a2 2 0 00-2.828 0L7 10.172V13h2.828l7.586-7.586a2 2 0 000-2.828z" />
                <path fillRule="evenodd" d="M2 6a2 2 0 012-2h4a1 1 0 010 2H4v10h10v-4a1 1 0 112 0v4a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" clipRule="evenodd" />
              </svg>
            </button>
          )}

          {/* Delete Button (only for own messages) */}
          {isOwnMessage && (
            <button
              onClick={() => setShowDeleteConfirm(true)}
              className="p-1.5 bg-gray-800 hover:bg-gray-750 rounded text-gray-400 hover:text-red-400 transition-colors"
              title="Delete message"
            >
              <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path fillRule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
            </button>
          )}
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 animate-fade-in">
          <div className="bg-gray-800 rounded-lg p-6 max-w-md w-full mx-4 animate-slide-up">
            <h3 className="text-xl font-bold text-white mb-2">Delete Message</h3>
            <p className="text-gray-300 mb-6">
              Are you sure you want to delete this message? This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setShowDeleteConfirm(false)}
                className="px-4 py-2 bg-gray-700 hover:bg-gray-600 text-white rounded transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={handleDelete}
                className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded transition-colors"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default MessageItem;
