

export class Logger {
    log(message: unknown, ...args: unknown[]) {
        if (import.meta.env.VITE_DEV_LOGGER) {
            console.log(message, ...args);
        }
    }
}

export const logger = new Logger();
