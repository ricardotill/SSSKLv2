import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { environment } from '../../../environments/environment';
import { TURNSTILE_HEADER, TurnstileService, isTurnstileFailure } from './turnstile.service';

describe('TurnstileService', () => {
  let originalSiteKey: string;

  beforeEach(() => {
    originalSiteKey = environment.turnstileSiteKey;
  });

  afterEach(() => {
    (environment as any).turnstileSiteKey = originalSiteKey;
    delete (window as any).turnstile;
  });

  it('is disabled and sends no header without a sitekey', () => {
    (environment as any).turnstileSiteKey = '';
    const service = TestBed.inject(TurnstileService);

    expect(service.enabled).toBe(false);
    expect(service.headers('token').has(TURNSTILE_HEADER)).toBe(false);
  });

  it('adds the token header when enabled', () => {
    (environment as any).turnstileSiteKey = 'site-key';
    const service = TestBed.inject(TurnstileService);

    expect(service.enabled).toBe(true);
    expect(service.headers('token').get(TURNSTILE_HEADER)).toBe('token');
    expect(service.headers(null).has(TURNSTILE_HEADER)).toBe(false);
  });

  it('resolves with an already loaded Turnstile API without adding a script', async () => {
    const api = { render: () => 'id', reset: () => {}, remove: () => {} };
    (window as any).turnstile = api;
    const service = TestBed.inject(TurnstileService);

    await expect(service.load()).resolves.toBe(api);
    expect(document.querySelector('script[src*="challenges.cloudflare.com"]')).toBeNull();
  });
});

describe('isTurnstileFailure', () => {
  it('recognises the API turnstile rejection', () => {
    const err = new HttpErrorResponse({ status: 403, error: { code: 'turnstile_failed' } });
    expect(isTurnstileFailure(err)).toBe(true);
  });

  it('does not treat the two-factor 403 as a Turnstile failure', () => {
    const err = new HttpErrorResponse({ status: 403, error: { detail: 'RequiresTwoFactor' } });
    expect(isTurnstileFailure(err)).toBe(false);
  });
});
