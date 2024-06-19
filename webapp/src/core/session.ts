import { nanoid } from 'nanoid';
import { loadDatabase, SharpbenchDb } from './database';
import { Logger } from './logger';
import { JobsRepository } from './JobsRepository';
import { Job, LogMessage } from './types';

export class Session {

    private _jobs: JobsRepository|null = null;
    private constructor(private _db: SharpbenchDb, private _id: string, private logger?: Logger) {
    }

    public static async loadSession(logger?: Logger): Promise<Session> {
        const db = await loadDatabase();
        const id = await this.getOrCreateSessionId(db);
        const session = new Session(db, id, logger);
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
            this._jobs = new JobsRepository(this._db, this.logger);
        }

        return this._jobs;
    }

    public createEvent(event: SessionEvent) {
        
    }

    
    public delete(): Promise<void> {
        throw new Error("Not Implemented");
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
    type: 'CreateLog';
    message: LogMessage;
}