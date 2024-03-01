export type RealtimeMessage = JobCompleteMessage | LogMessage;

export type JobCompleteMessage = {
  Type: "jobComplete";
  JobId: string;
  Job: {
    Id: string;
    ExitCode: number;
    MarkdownResult: string;
  }
}

export type LogMessage = {
  Type: "log";
  JobId: string;
  LogSource: "stdout" | "stderr";
  Message: string;
}

export type Job = {
  id: string,
  code: string,
  status: JobStatus,
  exitCode?: number,
  createdAt?: string,
  completedAt?: string,
  startedAt?: string,
  markdownReport?: string
}

export type JobStatus = 'Queued'|'Progress'|'Error'|'Completed';
