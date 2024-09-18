import { openDB, DBSchema, IDBPDatabase } from 'idb';
import { Job, StoredLogMessage } from './types';

const DB_NAME = 'sharpbench';
const DB_VERSION = 1;

export interface SharpbenchDbSchema extends DBSchema {
    jobs: {
        value: Job,
        key: string;
    },
    logs: {
        value: StoredLogMessage,
        key: string;
        indexes: { 'by-jobId': string }
    },
    session: {
        value: string;
        key: string;
    }
}

export async function loadDatabase() {
    const db = await openDB<SharpbenchDbSchema>(DB_NAME, DB_VERSION, {
        upgrade(db) {
            db.createObjectStore('jobs', {
                keyPath: 'id',
            });

            const logsStore = db.createObjectStore('logs', {
                keyPath: 'id',
            });

            logsStore.createIndex('by-jobId', 'jobId');

            db.createObjectStore('session');
        }
    });

    return db;
}

export type SharpbenchDb = IDBPDatabase<SharpbenchDbSchema>;