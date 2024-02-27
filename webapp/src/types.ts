export type RealtimeMessage = JobCompleteMessage | LogMessage;

export type JobCompleteMessage = {
  Type: "jobComplete";
  JobId: number;
  Job: {
    Id: number;
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
