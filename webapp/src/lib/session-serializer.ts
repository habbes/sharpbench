import { encode as messagePackEncode, decode as messagePackDecode } from "@msgpack/msgpack";

export type EncodeArgs = {
    code: string;
}

export function serializeSession(args: EncodeArgs) {
    const v1EncodeArgs: V1EncodedPayload = {
        v: 1,
        c: args.code
    };
    
    // Using message pack reduces the length of the serialized
    // string compared to using JSON.stringify, which
    // leads to slightly shorter shareable URLs
    const sourceBytes = messagePackEncode(v1EncodeArgs);

    const encoded = bytesToBase64(sourceBytes);
    return encoded;
}

export function deserializeSession(serialized: string) {
    // TODO: error handling in case the serialized string is not valid
    // and can't be correctly decoded
    const decodedBytes = base64ToBytes(serialized)
    
    const decodedPayload = messagePackDecode(decodedBytes) as EncodedPayload;
    return {
        v: decodedPayload.v,
        code: decodedPayload.c
    }
}

export type EncodedPayload = V1EncodedPayload;

export type V1EncodedPayload = {
    v: 1,
    c: string;
}

function bytesToBase64(bytes: Uint8Array) {
    const binString = Array.from(bytes, (byte) =>
      String.fromCodePoint(byte),
    ).join("");
    

    const serialized = btoa(binString);
    return serialized;
}

function base64ToBytes(base64: string) {
    const binString = atob(base64);
    // @ts-expect-error
    return Uint8Array.from(binString, (m) => m.codePointAt(0));
  }