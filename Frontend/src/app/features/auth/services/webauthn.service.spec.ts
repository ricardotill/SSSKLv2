import { TestBed } from '@angular/core/testing';
import { WebAuthnService } from './webauthn.service';
import { AuthService } from '../../../core/auth/auth.service';
import { of } from 'rxjs';
import { vi } from 'vitest';

describe('WebAuthnService', () => {
  let service: WebAuthnService;
  let authServiceMock: any;

  beforeEach(() => {
    authServiceMock = {
      getPasskeyCreationOptions: vi.fn(),
      registerPasskey: vi.fn(),
      getPasskeyRequestOptions: vi.fn(),
      loginWithPasskey: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        WebAuthnService,
        { provide: AuthService, useValue: authServiceMock }
      ]
    });
    service = TestBed.inject(WebAuthnService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should convert base64url to buffer and back', () => {
    const original = 'SGVsbG8td29ybGQ'; // "Hello-world" in base64url
    const buffer = service.base64urlToBuffer(original);
    const converted = service.bufferToBase64url(buffer.buffer as ArrayBuffer);
    expect(converted).toBe(original.replace(/-/g, '').replace(/_/g, '')); 
  });
});
