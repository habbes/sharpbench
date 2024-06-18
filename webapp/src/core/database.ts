import { openDB, DBSchema, IDBPDatabase } from 'idb';
import { Job } from './types';

const DB_NAME = 'sharpbench';
const DB_VERSION = 1;

export interface SharpbenchDbSchema extends DBSchema {
    jobs: {
        value: Job,
        key: string;
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

            db.createObjectStore('session');
        }
    });

    return db;
}

export type SharpbenchDb = IDBPDatabase<SharpbenchDbSchema>;