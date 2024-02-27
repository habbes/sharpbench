import { useState, useCallback, useEffect } from 'react';
import useWebSocket from 'react-use-websocket';

const SOCKET_URL = "ws://localhost:5176/jobs-ws";

type LogMessage = {
  JobId: string;
  LogSource: string;
  Message: string;
}

export function LogsContainer() {
  const [messages, setMessages] = useState<LogMessage[]>([]);
  const { lastJsonMessage } = useWebSocket(SOCKET_URL);

  useEffect(() => {
    if (!lastJsonMessage) return;
    setMessages([...messages, lastJsonMessage as LogMessage]);
  }, [lastJsonMessage]);

  console.log('last message', lastJsonMessage);

  return (
    <div className="font-mono p-4 text-sm">
      {
        messages.map(message => (
          <div>
            { message.Message }
          </div>
        ))
      }
    </div>
  );
}