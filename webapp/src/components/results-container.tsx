import { LogsContainer } from "@/components/logs-container";
import { ReportContainer } from "@/components/report-container";
import { JobCompleteMessage, LogMessage, RealtimeMessage } from "@/types";
import { useEffect, useState } from "react";

const SOCKET_URL = "ws://localhost:5176/jobs-ws";
import useWebSocket from 'react-use-websocket';


export function ResultsContainer() {
  const [selectedTab, setSelectedTab] = useState<'logs'|'results'>('logs');
  const [lastLogMessage, setLastLogMessage] = useState<LogMessage>();
  const [lastResultMessage, setLastResultMessage] = useState<JobCompleteMessage>();
  const { lastJsonMessage } = useWebSocket<RealtimeMessage>(SOCKET_URL);

  useEffect(() => {
    if (!lastJsonMessage) return;

    if (lastJsonMessage.Type === 'jobComplete') {
      setLastResultMessage(lastJsonMessage);
    } else {
      setLastLogMessage(lastJsonMessage);
    }
  }, [lastJsonMessage]);

  return (
    <div>
      <div className="h-[30px] flex items-center text-sm border-b border-b-gray-200 shadow-sm gap-4">
        <TabButton
          text={'Logs'}
          selected={selectedTab === 'logs'}
          onClick={() => setSelectedTab('logs')}
        />
        <TabButton
          text={'Results'}
          selected={selectedTab === 'results'}
          onClick={() => setSelectedTab('results')}
         />
      </div>
      <div className="overflow-y-auto" style={{height:"calc(100dvh - 80px)"}}>
        { selectedTab === 'logs' ?
          <LogsContainer lastMessage={lastLogMessage} /> :
          <ReportContainer message={lastResultMessage} />
        }
      </div>
    </div>
  )
}



function TabButton({ text, selected, onClick } : { text: string, selected?: boolean, onClick?: () => void }) {
  return (
    <div
      onClick={onClick}
      className={`hover:text-primary cursor-pointer h-full inline-flex items-center px-2 border-b-2 ${selected ? 'border-b-primary text-primary' : 'border-b-transparent'}`}
    >
      { text }
    </div>
  )
}
