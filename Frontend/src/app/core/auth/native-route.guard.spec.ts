import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, convertToParamMap, type ActivatedRouteSnapshot, type RouterStateSnapshot } from '@angular/router';
import { vi } from 'vitest';
import { nativePageGuard, nativeRouteGuard } from './native-route.guard';
import { FROM_APP_STATE } from '../services/native-bridge.service';
import { EmbeddedService } from '../services/embedded.service';

describe('nativeRouteGuard', () => {
  const run = () => TestBed.runInInjectionContext(() =>
    nativeRouteGuard('pos')({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
  const runSettings = (tab?: string) => TestBed.runInInjectionContext(() =>
    nativeRouteGuard('settings', (route) => !route.queryParamMap.has('tab'))(
      { queryParamMap: convertToParamMap(tab ? { tab } : {}) } as ActivatedRouteSnapshot,
      {} as RouterStateSnapshot));

  function setup(embedded: boolean, navigated: boolean) {
    const embeddedService = { isEmbedded: embedded, notifyNative: vi.fn() };
    const router = { navigated, parseUrl: vi.fn((url: string) => ({ url }) as unknown as UrlTree) };
    TestBed.configureTestingModule({
      providers: [
        { provide: EmbeddedService, useValue: embeddedService },
        { provide: Router, useValue: router },
      ],
    });
    return { embeddedService, router };
  }

  it('allows the route in a normal browser', () => {
    const { embeddedService } = setup(false, true);

    expect(run()).toBe(true);
    expect(embeddedService.notifyNative).not.toHaveBeenCalled();
  });

  it('hands the route to the app and stays on the current page', () => {
    const { embeddedService } = setup(true, true);

    expect(run()).toBe(false);
    expect(embeddedService.notifyNative).toHaveBeenCalledWith('pos');
  });

  it('falls back to /events on the initial navigation', () => {
    const { router } = setup(true, false);

    run();

    expect(router.parseUrl).toHaveBeenCalledWith('/events');
  });

  it('only hands over the URLs the condition selects', () => {
    const { embeddedService } = setup(true, true);

    expect(runSettings('security')).toBe(true);
    expect(embeddedService.notifyNative).not.toHaveBeenCalled();

    expect(runSettings()).toBe(false);
    expect(embeddedService.notifyNative).toHaveBeenCalledWith('settings');
  });
});

describe('nativePageGuard', () => {
  const run = (url: string) => TestBed.runInInjectionContext(() =>
    nativePageGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot));

  function setup(embedded: boolean, navigated: boolean, state?: Record<string, unknown>) {
    const embeddedService = { isEmbedded: embedded, notifyNative: vi.fn() };
    const router = { navigated, getCurrentNavigation: () => ({ extras: { state } }) };
    TestBed.configureTestingModule({
      providers: [
        { provide: EmbeddedService, useValue: embeddedService },
        { provide: Router, useValue: router },
      ],
    });
    return embeddedService;
  }

  it('allows the page in a normal browser', () => {
    const embedded = setup(false, true);

    expect(run('/events/1')).toBe(true);
    expect(embedded.notifyNative).not.toHaveBeenCalled();
  });

  it('hands a page the user navigated to back to the app, with its URL', () => {
    const embedded = setup(true, true);

    expect(run('/events/1')).toBe(false);
    expect(embedded.notifyNative).toHaveBeenCalledWith('open', '/events/1');
  });

  it('renders the page the app loaded in the WebView', () => {
    const embedded = setup(true, false);

    expect(run('/quotes?new=1')).toBe(true);
    expect(embedded.notifyNative).not.toHaveBeenCalled();
  });

  it('renders pages the app opened through the bridge', () => {
    const embedded = setup(true, true, { [FROM_APP_STATE]: true });

    expect(run('/quotes?edit=q1')).toBe(true);
    expect(embedded.notifyNative).not.toHaveBeenCalled();
  });
});
