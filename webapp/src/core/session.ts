import { nanoid } from 'nanoid';
import { loadDatabase, SharpbenchDb } from './database';
import { Logger } from './logger';
import { JobsRepository } from './JobsRepository';
import { Job, LogMessage } from './types';

export class Session {

    private _jobs: JobsRepository|null = null;
    private constructor(private _db: SharpbenchDb, private _id: string) {
    }

    public static async loadSession(logger?: Logger): Promise<Session> {
        logger?.log("loading db...");
        const db = await loadDatabase();
        logger?.log("loaded db", db);
        const id = await this.getOrCreateSessionId(db);
        const session = new Session(db, id);
        console.log('loaded session', id, session);
        return session;
    }

    public get id() {
        return this._id;
    }

    public get db() {
        return this._db;
    }

    public get jobs() {
        if (!this._jobs) {
            this._jobs = new JobsRepository(this._db);
        }

        return this._jobs;
    }

    public createEvent(event: SessionEvent) {
        switch (event.type) {
            case 'createJob':
                this.jobs.saveJob(event.job);
                return;
            case 'updateJob':
                this.jobs.saveJob(event.job);
                return;
            case 'createLog':
                this.jobs.saveLog(event.message);
                return;
        }
    }

    
    public async clear(): Promise<void> {
        await Promise.all([this._db.clear("jobs"), this._db.clear("logs")]);
    }

    private static async getOrCreateSessionId(db: SharpbenchDb) {
        const id = await db.get('session', 'id');
        if (id) {
           return id;
        }

        const newId = nanoid();
        
        await db.put('session', newId, 'id');
        return newId;
    }
}

export type SessionEvent = CreateJobEvent | UpdateJobEvent | CreateLogEvent;

export interface CreateJobEvent {
    type: 'createJob';
    job: Job;
}

export interface UpdateJobEvent {
    type: 'updateJob';
    job: Job;
}

export interface CreateLogEvent {
    type: 'createLog';
    message: LogMessage;
}
