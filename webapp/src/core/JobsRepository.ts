import { Job, LogMessage  } from "./types";
import { SharpbenchDb } from "./database";
import { Logger } from "./logger";
import { nanoid } from "nanoid";


export class JobsRepository {
    constructor(private db: SharpbenchDb, private logger?: Logger) {
    }

    async getJobs(): Promise<Job[]> {
        const jobs = await this.db.getAll('jobs');
        jobs.sort((a, b) => {
            if (a.createdAt && b.createdAt) {
                return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
            }

            return -1;
        });

        return jobs;
    }

    async saveJob(job: Job): Promise<void> {
        await this.db.put('jobs', job);
    }

    async saveLog(log: LogMessage): Promise<void> {
        await this.db.put("logs", {
            id: nanoid(),
            timestamp: new Date().getTime(),
            message: log.Message,
            jobId: log.JobId,
            logSource: log.LogSource
        });
    }

    async getLogs(): Promise<LogMessage[]> {
        const rawLogs = await this.db.getAll('logs');
        rawLogs.sort((a, b) => a.timestamp - b.timestamp);
        const logs = rawLogs.map<LogMessage>(l => ({
            LogSource: l.logSource,
            Message: l.message,
            JobId: l.jobId,
            Type: 'log'
        }));

        return logs;
    }
}