import { LogMessage } from '@/types';
import { useState, useEffect } from 'react';

export function LogsContainer({ lastMessage } : { lastMessage? : LogMessage }) {
  const [messages, setMessages] = useState<LogMessage[]>([]);

  useEffect(() => {
    if (!lastMessage) return;
    setMessages([...messages, lastMessage]);
  }, [lastMessage]);

  console.log('last message', lastMessage);

  return (
    <div className="font-mono p-4 text-sm">
      {
        messages.map((message, index) => (
          <div key={index}>
            { message.Message }
          </div>
        ))
      }
    </div>
  );
}