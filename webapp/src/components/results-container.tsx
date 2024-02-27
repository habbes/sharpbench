import { LogsContainer } from "@/components/logs-container";

export function ResultsContainer() {
  return (
    <div>
      <div className="h-[30px] flex items-center px-4 text-sm border-b border-b-gray-200 shadow-sm">
        Logs
      </div>
      <div className="overflow-y-auto" style={{height:"calc(100dvh - 80px)"}}>
        <LogsContainer />
      </div>
    </div>
  )
}