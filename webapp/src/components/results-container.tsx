import { LogsContainer } from "@/components/logs-container";
import { useState } from 'react';
import { ReportContainer } from "@/components/report-container";
import { Job, LogMessage } from "@/core/types";

interface ResultsContainerProps {
  logs: LogMessage[];
  job?: Job;
}

export function ResultsContainer({ job, logs } : ResultsContainerProps) {
  const [selectedTab, setSelectedTab] = useState<'logs'|'results'>('logs');
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
          <LogsContainer logs={logs} /> :
          (job ? <ReportContainer job={job} /> : <div></div>)
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
