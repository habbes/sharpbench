

export type EncodeArgs = {
    code: string;
}

export function serializeSession(args: EncodeArgs) {
    const v1EncodeArgs: V01EncodedPayload = {
        v: '0.1',
        code: args.code
    };

    const encoded = btoa(JSON.stringify(v1EncodeArgs));
    return encoded;
}

export function deserializeSession(serialized: string) {
    const decodedString = atob(serialized);
    const decodedPayload = JSON.parse(decodedString);
    return decodedPayload as EncodedPayload;
}

export type EncodedPayload = V01EncodedPayload;

export type V01EncodedPayload = {
    v: '0.1',
    code: string;
}