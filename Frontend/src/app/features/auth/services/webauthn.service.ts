import { Injectable, inject } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { Observable, from, throwError } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class WebAuthnService {
  private authService = inject(AuthService);

  /**
   * Converts a Base64URL string to a Uint8Array.
   */
  public base64urlToBuffer(base64url: string): Uint8Array {
    const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes;
  }

  /**
   * Converts an ArrayBuffer to a Base64URL string.
   */
  public bufferToBase64url(buffer: ArrayBuffer): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
  }

  /**
   * Initiates the Passkey registration flow.
   */
  public register(name?: string): Observable<any> {
    return this.authService.getPasskeyCreationOptions().pipe(
      switchMap((options: any) => {
        // Handle both wrapped (FIDO2-Net-Lib) and unwrapped (.NET 10 Identity) options
        const publicKey = options.publicKey || options;

        const credentialOptions: PublicKeyCredentialCreationOptions = {
          ...publicKey,
          challenge: this.base64urlToBuffer(publicKey.challenge),
          user: {
            ...publicKey.user,
            id: this.base64urlToBuffer(publicKey.user.id)
          },
          excludeCredentials: publicKey.excludeCredentials?.map((cred: any) => ({
            ...cred,
            id: this.base64urlToBuffer(cred.id)
          }))
        };

        return from(navigator.credentials.create({ publicKey: credentialOptions })).pipe(
          switchMap((credential: any) => {
            if (!credential) {
              return throwError(() => new Error('Passkey registration cancelled or failed.'));
            }

            const response = credential.response;
            const credentialData = {
              id: credential.id,
              type: credential.type,
              clientExtensionResults: credential.getClientExtensionResults(),
              response: {
                attestationObject: this.bufferToBase64url(response.attestationObject),
                clientDataJSON: this.bufferToBase64url(response.clientDataJSON),
                transports: credential.getTransports ? credential.getTransports() : []
              },
              authenticatorAttachment: (credential as any).authenticatorAttachment || null
            };
            return this.authService.registerPasskey(credentialData, name);
          }),
          catchError(err => {
            console.error('Passkey creation error:', err);
            return throwError(() => err);
          })
        );
      })
    );
  }

  /**
   * Initiates the Passkey login flow.
   */
  public login(userName: string): Observable<any> {
    return this.authService.getPasskeyRequestOptions(userName).pipe(
      switchMap((options: any) => {
        // Handle both wrapped (FIDO2-Net-Lib) and unwrapped (.NET 10 Identity) options
        const publicKey = options.publicKey || options;

        const credentialOptions: PublicKeyCredentialRequestOptions = {
          ...publicKey,
          challenge: this.base64urlToBuffer(publicKey.challenge),
          allowCredentials: publicKey.allowCredentials?.map((cred: any) => ({
            ...cred,
            id: this.base64urlToBuffer(cred.id)
          }))
        };

        return from(navigator.credentials.get({ publicKey: credentialOptions })).pipe(
          switchMap((credential: any) => {
            if (!credential) {
              return throwError(() => new Error('Passkey login cancelled or failed.'));
            }

            const response = credential.response as AuthenticatorAssertionResponse;
            const assertionData = {
              id: credential.id,
              type: credential.type,
              clientExtensionResults: credential.getClientExtensionResults(),
              response: {
                authenticatorData: this.bufferToBase64url(response.authenticatorData),
                clientDataJSON: this.bufferToBase64url(response.clientDataJSON),
                signature: this.bufferToBase64url(response.signature),
                userHandle: response.userHandle ? this.bufferToBase64url(response.userHandle) : null
              },
              authenticatorAttachment: (credential as any).authenticatorAttachment || null
            };
            return this.authService.loginWithPasskey(assertionData);
          }),
          catchError(err => {
            console.error('Passkey assertion error:', err);
            return throwError(() => err);
          })
        );
      })
    );
  }

  private boxAttestation(attestation: PublicKeyCredential): any {
    const response = attestation.response as AuthenticatorAttestationResponse;
    return {
      id: attestation.id,
      rawId: this.bufferToBase64url(attestation.rawId),
      type: attestation.type,
      response: {
        attestationObject: this.bufferToBase64url(response.attestationObject),
        clientDataJSON: this.bufferToBase64url(response.clientDataJSON),
        transports: response.getTransports ? response.getTransports() : []
      },
      authenticatorAttachment: attestation.authenticatorAttachment
    };
  }

  private boxAssertion(assertion: PublicKeyCredential): any {
    const response = assertion.response as AuthenticatorAssertionResponse;
    return {
      id: assertion.id,
      rawId: this.bufferToBase64url(assertion.rawId),
      type: assertion.type,
      response: {
        authenticatorData: this.bufferToBase64url(response.authenticatorData),
        clientDataJSON: this.bufferToBase64url(response.clientDataJSON),
        signature: this.bufferToBase64url(response.signature),
        userHandle: response.userHandle ? this.bufferToBase64url(response.userHandle) : null
      },
      authenticatorAttachment: assertion.authenticatorAttachment
    };
  }
}
