import { DotFilledIcon, ClockIcon, CheckIcon, Cross2Icon, CheckCircledIcon } from "@radix-ui/react-icons";

type Job = {
  id: string,
  startedAt: Date,
  completedAt?: Date,
  createdAt: Date,
  status: 'queued'|'progress'|'completed'|'error'
};

export function JobsSidebar() {
  const jobs: Job[] = [];
  for (let i = 0; i < 100; i++) {
    jobs.push({
      id: `d8c38ce2-0e1e-4ff5-bd20-cfc53520f05f${i}`,
      status: ['queued', 'progress', 'completed', 'error'][i % 4] as Job["status"],
      startedAt: new Date(),
      completedAt: new Date(),
      createdAt: new Date(),
    });
  }

  return (
    <aside className="overflow-y-auto h-full">
      {
        jobs.map(job => {
          return <JobItem key={job.id} job={job} />
        })
      }
    </aside>
  )
}

function JobItem({ job } : { job: Job }) {
  return (
    <div className="px-4 py-2 border-b border-b-gray-200 cursor-pointer flex flex-col gap-2 items-stretch hover:bg-gray-100">
      <div className="text-xs">
        {job.id}
      </div>
      <div className="flex justify-between">
        <div className="flex items-center">
          <JobStatus status={job.status} />
        </div>
        <div className="text-xs text-gray-500">
          { job.status === 'progress' &&
            <span>Started at { job.startedAt.toLocaleDateString() }</span>
          }
          {
            (job.status === 'completed' || job.status === 'error') &&
            <span>Completed at { job.completedAt?.toLocaleDateString() }</span>
          }
          { job.status === 'queued' &&
            <span>Created at { job.createdAt.toLocaleDateString() }</span>
          }
        </div>
      </div>
    </div>
  )
}

function JobStatus({ status } : { status: Job["status"]}) {
  return (
    status === 'queued' ?
      <div className="inline-flex gap-1 items-center text-xs text-gray-500"><ClockIcon /> Queued</div>
    : status === 'progress' ?
      <div className="inline-flex gap-1 items-center text-xs text-orange-500">
        <DotFilledIcon color="orange" />
        Running
      </div>
    : status === 'completed' ?
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