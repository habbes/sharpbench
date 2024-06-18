import { nanoid } from 'nanoid';
import { loadDatabase, SharpbenchDb } from './database';
import { Logger } from './logger';
import { JobsRepository } from './JobsRepository';


export class Session {

    private _jobs: JobsRepository|null = null;
    private constructor(private _db: SharpbenchDb, private _id: string, private logger?: Logger) {
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

    public static async loadSession(logger?: Logger): Promise<Session> {
        const db = await loadDatabase();
        const id = await this.getOrCreateSessionId(db);
        const session = new Session(db, id, logger);
        return session;
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
