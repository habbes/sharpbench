import { Job } from "./types";
import { SharpbenchDb } from "./database";
import { Logger } from "./logger";


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
}