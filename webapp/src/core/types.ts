export type RealtimeMessage = JobCompleteMessage | LogMessage;

export type JobCompleteMessage = {
  Type: "jobComplete";
  JobId: string;
  Job: {
    Id: string;
    ExitCode: number;
    MarkdownReport: string;
  }
}

export type LogMessage = {
  Type: "log";
  JobId: string;
  LogSource: "stdout" | "stderr";
  Message: string;
}

export type StoredLogMessage = {
  jobId: string;
  logSource: "stdout" | "stderr";
  message: string;
  id: string;
  timestamp: number;
}

export type Job = {
  id: string,
  clientId: string,
  code: string,
  status: JobStatus,
  exitCode?: number,
  createdAt?: string,
  completedAt?: string,
  startedAt?: string,
  markdownReport?: string
}

export type JobStatus = 'Queued'|'Progress'|'Error'|'Completed';
