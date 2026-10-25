import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TURNSTILE_HEADER, TurnstileService, isTurnstileFailure } from './turnstile.service';

describe('TurnstileService', () => {
  let service: TurnstileService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(TurnstileService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    delete (window as any).turnstile;
  });

  it('is disabled until the config arrives and sends no header', () => {
    expect(service.enabled()).toBe(false);
    expect(service.blocksSubmit(null)).toBe(false);
    expect(service.headers('token').has(TURNSTILE_HEADER)).toBe(false);
  });

  it('loads the public config once and enables itself', () => {
    service.loadConfig();
    service.loadConfig();
    http.expectOne('/api/v1/GlobalSettings/public/turnstile').flush({ enabled: true, siteKey: 'site-key' });

    expect(service.enabled()).toBe(true);
    expect(service.siteKey()).toBe('site-key');
    expect(service.blocksSubmit(null)).toBe(true);
    expect(service.blocksSubmit('token')).toBe(false);
    expect(service.headers('token').get(TURNSTILE_HEADER)).toBe('token');
  });

  it('stays disabled when the API says so or the call fails', () => {
    service.loadConfig();
    http.expectOne('/api/v1/GlobalSettings/public/turnstile').flush('boom', { status: 500, statusText: 'Error' });

    expect(service.enabled()).toBe(false);

    // A failed fetch may be retried by the next form
    service.loadConfig();
    http.expectOne('/api/v1/GlobalSettings/public/turnstile').flush({ enabled: false, siteKey: null });
    expect(service.enabled()).toBe(false);
  });

  it('resolves with an already loaded Turnstile API without adding a script', async () => {
    const api = { render: () => 'id', reset: () => {}, remove: () => {} };
    (window as any).turnstile = api;

    await expect(service.load()).resolves.toBe(api);
    expect(document.querySelector('script[src*="challenges.cloudflare.com"]')).toBeNull();
  });

  it('injects the script once and resolves when it loads', async () => {
    const first = service.load();
    const second = service.load();
    const script = document.querySelector<HTMLScriptElement>('script[src*="challenges.cloudflare.com"]')!;
    expect(script.src).toContain('render=explicit');

    const api = { render: () => 'id', reset: () => {}, remove: () => {} };
    (window as any).turnstile = api;
    script.onload!(new Event('load'));

    await expect(first).resolves.toBe(api);
    await expect(second).resolves.toBe(api);
    script.remove();
  });

  it('rejects and allows a retry when the script fails to load', async () => {
    const attempt = service.load();
    const script = document.querySelector<HTMLScriptElement>('script[src*="challenges.cloudflare.com"]')!;
    script.onerror!(new Event('error'));

    await expect(attempt).rejects.toThrow('Turnstile script failed to load');
    script.remove();
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
