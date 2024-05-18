import { LogMessage } from '@/types';

export function LogsContainer({ logs } : { logs: LogMessage[] }) {
  return (
    <div className="font-mono p-4 text-xs">
      {
        logs.map((message, index) => (
          <div key={index} className={message.LogSource === 'stderr' ? 'text-red-500' : ''}>
            { message.Message }
          </div>
        ))
      }
    </div>
  );
}
