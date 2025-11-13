import { useEffect, useState } from 'react';
import { signalRService } from '../../services/signalr';
import * as signalR from '@microsoft/signalr';

const ConnectionStatus = () => {
  const [status, setStatus] = useState<signalR.HubConnectionState | null>(null);

  useEffect(() => {
    // Check connection status every 2 seconds
    const interval = setInterval(() => {
      setStatus(signalRService.connectionState);
    }, 2000);

    return () => clearInterval(interval);
  }, []);

  if (!status || status === signalR.HubConnectionState.Connected) {
    return null; // Don't show anything if connected
  }

  const getStatusInfo = () => {
    switch (status) {
      case signalR.HubConnectionState.Connecting:
        return { text: 'Connecting...', color: 'bg-yellow-500' };
      case signalR.HubConnectionState.Reconnecting:
        return { text: 'Reconnecting...', color: 'bg-yellow-500' };
      case signalR.HubConnectionState.Disconnected:
        return { text: 'Disconnected', color: 'bg-red-500' };
      default:
        return { text: 'Unknown', color: 'bg-gray-500' };
    }
  };

  const { text, color } = getStatusInfo();

  return (
    <div className="fixed bottom-4 left-4 z-50 animate-slide-up">
      <div className="bg-gray-800 border border-gray-700 rounded-lg px-4 py-2 shadow-lg flex items-center space-x-2">
        <div className={`w-2 h-2 rounded-full ${color} animate-pulse`} />
        <span className="text-sm text-gray-300">{text}</span>
      </div>
    </div>
  );
};

export default ConnectionStatus;
