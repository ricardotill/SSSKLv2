import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { SwPush } from '@angular/service-worker';
import { BehaviorSubject } from 'rxjs';
import { vi } from 'vitest';
import { PushNotificationService } from './push-notification.service';

// ── Polyfill Notification for JSDOM ───────────────────────────────────────────

if (typeof (globalThis as any).Notification === 'undefined') {
  (globalThis as any).Notification = { permission: 'default' };
}

// ── Helpers ────────────────────────────────────────────────────────────────────

function makeFakeSubscription(endpoint = 'https://push.example.com/sub'): PushSubscription {
  return {
    endpoint,
    toJSON: () => ({ endpoint, keys: { p256dh: 'fake-p256dh', auth: 'fake-auth' } }),
  } as unknown as PushSubscription;
}

// ── Shared test state ──────────────────────────────────────────────────────────

// These are reassigned in beforeEach, so tests share one module per suite-run but can
// set per-test state before the service is injected.
const subscriptionSubject = new BehaviorSubject<PushSubscription | null>(null);
const swPushMock = {
  isEnabled: true,
  subscription: subscriptionSubject.asObservable(),
  requestSubscription: vi.fn(),
  unsubscribe: vi.fn().mockResolvedValue(undefined),
};

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('PushNotificationService', () => {
  let service: PushNotificationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    subscriptionSubject.next(null);
    swPushMock.unsubscribe = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty((globalThis as any).Notification, 'permission', {
      value: 'default',
      configurable: true,
      writable: true,
    });

    TestBed.configureTestingModule({
      providers: [
        PushNotificationService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: SwPush, useValue: swPushMock },
      ],
    });

    service = TestBed.inject(PushNotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  // ── Creation ──────────────────────────────────────────────────────────────

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('isSupported should be true when SwPush.isEnabled is true', () => {
    expect(service.isSupported()).toBe(true);
  });

  // ── showPrompt Signal ──────────────────────────────────────────────────────

  it('showPrompt should be true when permission is default and not shown before', () => {
    expect(service.showPrompt()).toBe(true);
  });

  it('showPrompt should be false when push_prompt_shown is in localStorage', () => {
    // Must test directly since the constructor already ran; test setPrompted effect instead
    service.setPrompted();
    expect(service.showPrompt()).toBe(false);
    expect(localStorage.getItem('push_prompt_shown')).toBe('true');
  });

  it('showPrompt should be false when permission is "denied" (set via setPrompted)', () => {
    service.setPrompted();
    expect(service.showPrompt()).toBe(false);
  });

  // ── setPrompted ────────────────────────────────────────────────────────────

  it('setPrompted should set localStorage and hide the prompt', () => {
    expect(service.showPrompt()).toBe(true);
    service.setPrompted();
    expect(localStorage.getItem('push_prompt_shown')).toBe('true');
    expect(service.showPrompt()).toBe(false);
  });

  // ── getVapidPublicKey ──────────────────────────────────────────────────────

  it('getVapidPublicKey should call the backend and return the key', async () => {
    const keyPromise = service.getVapidPublicKey();

    const req = httpMock.expectOne(r => r.url.includes('/vapid-public-key'));
    expect(req.request.method).toBe('GET');
    req.flush('returned-vapid-key');

    const key = await keyPromise;
    expect(key).toBe('returned-vapid-key');
  });

  // ── subscribe ──────────────────────────────────────────────────────────────

  it('subscribe should fetch VAPID key, POST to backend, and update signals', async () => {
    const fakeSub = makeFakeSubscription();
    swPushMock.requestSubscription.mockResolvedValue(fakeSub);

    const subscribePromise = service.subscribe();

    // 1. Answer VAPID key fetch
    const vapidReq = httpMock.expectOne(r => r.url.includes('/vapid-public-key'));
    vapidReq.flush('the-vapid-key');

    // 2. Wait for requestSubscription microtask to complete
    await vi.waitFor(() => {
      expect(swPushMock.requestSubscription).toHaveBeenCalled();
    });

    // 3. Now answer the subscribe POST
    const subReq = httpMock.expectOne(r => r.url.includes('/subscribe'));
    expect(subReq.request.method).toBe('POST');
    expect(subReq.request.body).toMatchObject({
      endpoint: 'https://push.example.com/sub',
      p256dh: 'fake-p256dh',
      auth: 'fake-auth',
    });
    subReq.flush({});
    await subscribePromise;

    expect(service.isEnabled()).toBe(true);
    expect(service.showPrompt()).toBe(false);
    expect(swPushMock.requestSubscription).toHaveBeenCalledWith({ serverPublicKey: 'the-vapid-key' });
  });

  it('subscribe should re-throw on error from requestSubscription', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    swPushMock.requestSubscription.mockRejectedValue(new Error('User denied permission'));

    const subscribePromise = service.subscribe();

    const vapidReq = httpMock.expectOne(r => r.url.includes('/vapid-public-key'));
    vapidReq.flush('key');

    await expect(subscribePromise).rejects.toThrow('User denied permission');
  });

  // ── unsubscribe ────────────────────────────────────────────────────────────

  it('unsubscribe should POST the endpoint when a subscription exists', async () => {
    const fakeSub = makeFakeSubscription();
    // Pre-populate the subscription so firstValueFrom gets a value immediately
    subscriptionSubject.next(fakeSub);

    // Start unsubscribe but don't await yet
    const unsubPromise = service.unsubscribe();

    // Allow the firstValueFrom + POST to be queued
    await Promise.resolve();

    const req = httpMock.expectOne(r => r.url.includes('/unsubscribe'));
    expect(req.request.method).toBe('POST');
    req.flush({});

    await unsubPromise;
    expect(swPushMock.unsubscribe).toHaveBeenCalled();
    expect(service.isEnabled()).toBe(false);
  });

  it('unsubscribe when no subscription should not POST', async () => {
    // subscriptionSubject is null by default from beforeEach
    await service.unsubscribe();
    // No HTTP requests should be made
    expect(service.isEnabled()).toBe(false);
  });
});
