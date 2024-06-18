import { Job } from "./types";
import { SharpbenchDb } from "./database";
import { Logger } from "./logger";


export class JobsRepository {
    constructor(private db: SharpbenchDb, private logger?: Logger) {
    }

    getJobs(): Promise<Job[]> {
        return this.db.getAll('jobs');
    }

    async saveJob(job: Job): Promise<void> {
        await this.db.put('jobs', job);
    }
}