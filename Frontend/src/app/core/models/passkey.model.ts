export interface PasskeyDto {
    credentialIdBase64: string;
    displayName: string | null;
    createdAt: string;
}

export interface DeletePasskeyRequest {
    credentialIdBase64: string;
}
