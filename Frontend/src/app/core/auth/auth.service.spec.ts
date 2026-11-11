import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { describe, it, expect, afterEach, vi } from 'vitest';
import { AuthService } from './auth.service';
import { EmbeddedService } from '../services/embedded.service';

describe('AuthService', () => {
  function setup(embedded: boolean) {
    const embeddedService = { isEmbedded: embedded, notifyNative: vi.fn() };
    const router = { navigate: vi.fn(() => Promise.resolve(true)) };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: EmbeddedService, useValue: embeddedService },
        { provide: Router, useValue: router },
      ],
    });
    return { service: TestBed.inject(AuthService), embeddedService, router };
  }

  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('exposes whether it runs inside the mobile app', () => {
    expect(setup(true).service.isEmbedded).toBe(true);
  });

  it('logout clears the tokens, notifies the app and navigates home', () => {
    localStorage.setItem('access_token', 'a');
    localStorage.setItem('refresh_token', 'r');
    sessionStorage.setItem('access_token', 'a');
    const { service, embeddedService, router } = setup(true);

    service.logout();

    expect(service.getAccessToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
    expect(embeddedService.notifyNative).toHaveBeenCalledWith('logout');
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });
});
