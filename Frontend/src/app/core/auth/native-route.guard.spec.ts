import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, convertToParamMap, type ActivatedRouteSnapshot, type RouterStateSnapshot } from '@angular/router';
import { vi } from 'vitest';
import { nativeRouteGuard } from './native-route.guard';
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
