import { useState, useEffect, useMemo, useCallback } from 'react'
import { PlayIcon } from '@radix-ui/react-icons';
import './App.css'
import { Button } from "@/components/ui/button";
import { CodeEditor } from "@/components/code-editor";
import { ResultsContainer } from "@/components/results-container";
import { JobsSidebar } from "@/components/jobs-sidebar";
import { DoubleArrowRightIcon, DoubleArrowLeftIcon } from "@radix-ui/react-icons";
import { INITIAL_CODE } from "./initial-code";
import { Job, LogMessage, RealtimeMessage } from './types';
import useWebSocket from 'react-use-websocket';
import { EncodeArgs, deserializeSession, serializeSession } from './lib';

// TODO this should be configure using env vars
const API_URL = import.meta.env.VITE_API_BASE_URL || "http://localhost:5176";
const WS_URL = import.meta.env.VITE_WS_BASE_URL || API_URL.replace("http", "ws");
const EDITOR_SERVICE_URL = `${WS_URL}/mirrorsharp`;
const JOB_UPDATES_URL = `${WS_URL}/jobs-ws`;

export function App() {
  const [initialCode] = useState(() => {
    const decoded = decodeUrlSession();
    console.log('initial code', decoded);
    return decoded ? decoded.code : INITIAL_CODE
  });
  const [code, setCode] = useState(initialCode);
  const [isShowingJobsSidebar, setIsShowingJobsSidebar] = useState(false);
  const [jobs, setJobs] = useState<Job[]>([]);
  const [logs, setLogs] = useState<LogMessage[]>([]);
  const [currentJobId, setCurrentJobId] = useState<string>();
  const { lastJsonMessage } = useWebSocket<RealtimeMessage>(JOB_UPDATES_URL, {
    retryOnError: true,
    shouldReconnect: () => true
  });

  const currentLogs = useMemo(() => {
    if (!currentJobId) return [];
    return logs.filter(log => log.JobId === currentJobId);
  }, [logs, currentJobId]);

  const currentJob = jobs.find(j => j.id === currentJobId);

  useEffect(() => {
    if (!lastJsonMessage) return;
    console.log('received message', lastJsonMessage);
    if (lastJsonMessage.Type === 'jobComplete') {
      // TODO: update job from API instead
      const jobIndex = jobs.findIndex(j => j.id === lastJsonMessage.JobId);

      const result = updateJob(jobs, jobIndex, {
        status: lastJsonMessage.Job.ExitCode === 0 ? 'Completed' : 'Error',
        markdownReport: lastJsonMessage.Job.MarkdownReport,
        completedAt: new Date().toString()
      });
      
      if (!result.success) return;
      setJobs(result.jobs);
    } else {
      setLogs(logs => [...logs, lastJsonMessage]);
      // TODO: the update should be done from the API
      // set the job to running if this is the first log message
      const jobIndex = jobs.findIndex(j => j.id === lastJsonMessage.JobId);
      if (jobIndex === -1) return;
      const job = jobs[jobIndex];
      if (job.status !== 'Queued') return;
      const result = updateJob(jobs, jobIndex, { status: 'Progress', startedAt: new Date().toString() });
      if (!result.success) return;
      setJobs(result.jobs);
    }
  }, [lastJsonMessage]);

  async function handleRun() {
    if (!code) return;
    const job = await submitCodeRun(code);
    setJobs([job, ...jobs]);
    setIsShowingJobsSidebar(true);
    setCurrentJobId(job.id);
  }

  function toggleShowJobsSidebar() {
    setIsShowingJobsSidebar(!isShowingJobsSidebar);
  }

  const handleCodeChange = useCallback((newCode: string) => {
    encodeSessionInUrl({ code: newCode })
    setCode(newCode);
  }, [setCode]);

  return (
    <main className="h-screen bg-red flex flex-col">
      <div className="h-[50px] pr-2 flex items-center border-b border-b-gray-200 shadow-sm justify-between">
        <div className="flex items-center h-full">
          {!isShowingJobsSidebar ?
            (
              <div onClick={toggleShowJobsSidebar}
                className="px-4 border-r border-r-gray-200 mr-4 h-full flex items-center cursor-pointer"
                title="Reveal job history"
              >
                <DoubleArrowRightIcon />
              </div>
            ) :
            (
              <div onClick={toggleShowJobsSidebar}
                className="flex w-[300px] h-full px-4 items-center border-r border-r-gray-200 justify-between mr-4"
              >
                <div className="text-sm font-semibold">
                  Jobs History
                </div>
                <div className="cursor-pointer" title="Hide job history">
                  <DoubleArrowLeftIcon />
                </div>
              </div>
            )
          }
          <Button onClick={handleRun} className="inline-flex flex gap-2 items-center"><PlayIcon /> Run</Button>
        </div>
        <div>
          <div className="mr-5">
            <span className="font-semibold">sharpbench</span>
          </div>
        </div>
      </div>
      <div className="flex flex-1" style={{height:"calc(100dvh - 50px)"}}>
        {
          isShowingJobsSidebar &&
          (
            <div className="w-[300px] border-r border-r-gray-200 h-full">
              <JobsSidebar jobs={jobs} onSelectJob={setCurrentJobId} selectedJobId={currentJobId}/>
            </div>
          )
        }
        <div className="flex-1 h-full">
          <CodeEditor
            serverUrl={EDITOR_SERVICE_URL}
            code={currentJob ? currentJob.code : initialCode}
            onTextChange={handleCodeChange}
          />
        </div>
        <div className="flex-1 h-full border-l border-l-gray-200">
          <ResultsContainer logs={currentLogs} job={currentJob} />
        </div>
      </div>
    </main>
  )
}

async function submitCodeRun(code: string): Promise<Job> {
  console.log('submitting code run', code);
  const resp = await fetch(`${API_URL}/run`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      code: code
    }),
    mode: 'cors'
  });

  const data = await resp.json();
  console.log('code run result', data);
  const job = data as Job; // TODO these fields should be included from API
  job.createdAt = new Date().toString();
  return job;
}

function updateJob(jobs: Job[], index: number, update: Partial<Job>) {
  if (index < 0 ) {
    return ({ success: false, jobs });
  }

  const job = jobs[index];
  const updated = { ...job, ...update };
  const updatedJobs = [...jobs];
  updatedJobs[index] = updated;
  return { success: true, jobs: updatedJobs }
}

function decodeUrlSession() {
  const hash = window.location.hash;
  // hash is in the form #s:{serializedSession}
  const [, serializedSession] = hash.split(':', 2);
  if (!serializedSession) {
    return;
  }

  const decoded = deserializeSession(serializedSession);
  return decoded;
}

function encodeSessionInUrl(args : EncodeArgs) {
  const serialized = serializeSession(args);
  window.history.replaceState(null, "", `#s:${serialized}`);
}