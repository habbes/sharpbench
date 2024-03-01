import { Job } from "@/types";
import { DotFilledIcon, ClockIcon, Cross2Icon, CheckCircledIcon } from "@radix-ui/react-icons";


type JobsSidebarProps = {
  jobs: Job[],
  onSelectJob?: (jobId: string) => unknown,
  selectedJobId?: string;
}

export function JobsSidebar({ jobs, onSelectJob, selectedJobId } : JobsSidebarProps) {
  return (
    <aside className="overflow-y-auto h-full">
      {
        jobs.map(job => {
          return <JobItem
            key={job.id}
            job={job}
            selected={selectedJobId === job.id}
            onClick={() => onSelectJob && onSelectJob(job.id)}
          />
        })
      }
    </aside>
  )
}

function JobItem({ job, onClick, selected } : { job: Job, selected?: boolean, onClick: () => unknown }) {
  return (
    <div
      onClick={onClick}
      className={`px-4 py-2 border-b border-b-gray-200 cursor-pointer flex flex-col gap-2 items-stretch hover:bg-gray-50 ${selected ? 'bg-violet-50' : ''}`}
    >
      <div className="text-xs">
        {job.id}
      </div>
      <div className="flex justify-between">
        <div className="flex items-center">
          <JobStatus status={job.status} />
        </div>
        <div className="text-xs text-gray-500">
          { job.status === 'Progress' &&
            <span>Started on { formatDateString(job.startedAt!) }</span>
          }
          {
            (job.status === 'Completed' || job.status === 'Error') &&
            <span>Completed on { formatDateString(job.completedAt!) }</span>
          }
          { job.status === 'Queued' &&
            <span>Created on { formatDateString(job.createdAt!) }</span>
          }
        </div>
      </div>
    </div>
  )
}

function JobStatus({ status } : { status: Job["status"]}) {
  return (
    status === 'Queued' ?
      <div className="inline-flex gap-1 items-center text-xs text-gray-500"><ClockIcon /> Queued</div>
    : status === 'Progress' ?
      <div className="inline-flex gap-1 items-center text-xs text-orange-500">
        <DotFilledIcon color="orange" />
        Running
      </div>
    : status === 'Completed' ?
      <div className="inline-flex gap-1 items-center text-xs text-green-600">
        <CheckCircledIcon color="green" />
        Completed
      </div>
      
    :
      <div className="inline-flex gap-1 items-center text-xs text-red-500">
        <Cross2Icon color="red" />
        Failed
      </div>
  )
}

function formatDateString(dateString: string) {
  const date = new Date(dateString);
  return `${date.toLocaleDateString()} at ${date.toLocaleTimeString()}`
}