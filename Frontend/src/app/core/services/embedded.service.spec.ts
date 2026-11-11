import { TestBed } from '@angular/core/testing';
import { DOCUMENT } from '@angular/core';
import { vi } from 'vitest';
import { EmbeddedService, isEmbedded } from './embedded.service';

function fakeWindow(options: { global?: boolean; userAgent?: string } = {}) {
  const win: Record<string, unknown> = {
    navigator: { userAgent: options.userAgent ?? 'Mozilla/5.0' },
    location: { assign: vi.fn() },
  };
  if (options.global) {
    win['SSSKL_APP'] = { version: '3.12.3' };
  }
  return win as unknown as Window & { location: { assign: ReturnType<typeof vi.fn> } };
}

describe('isEmbedded', () => {
  it('is false in a normal browser', () => {
    expect(isEmbedded(fakeWindow())).toBe(false);
  });

  it('detects the global the app injects', () => {
    expect(isEmbedded(fakeWindow({ global: true }))).toBe(true);
  });

  it('detects the app user agent token', () => {
    expect(isEmbedded(fakeWindow({ userAgent: 'Mozilla/5.0 (Linux; Android 15) SSSKLApp/3.12.3' }))).toBe(true);
  });

  it('is false without a window', () => {
    expect(isEmbedded(null)).toBe(false);
  });
});

describe('EmbeddedService', () => {
  function setup(win: Window) {
    TestBed.configureTestingModule({
      providers: [{ provide: DOCUMENT, useValue: { defaultView: win } }],
    });
    return TestBed.inject(EmbeddedService);
  }

  it('navigates to the app scheme when embedded', () => {
    const win = fakeWindow({ global: true });
    const service = setup(win);

    service.notifyNative('pos');

    expect(service.isEmbedded).toBe(true);
    expect(win.location.assign).toHaveBeenCalledWith('ssskl-app://pos');
  });

  it('passes the route to show natively', () => {
    const win = fakeWindow({ global: true });
    const service = setup(win);

    service.notifyNative('open', '/events/1?x=a b');

    expect(win.location.assign).toHaveBeenCalledWith('ssskl-app://open?path=%2Fevents%2F1%3Fx%3Da%20b');
  });

  it('does nothing in a normal browser', () => {
    const win = fakeWindow();
    const service = setup(win);

    service.notifyNative('logout');

    expect(service.isEmbedded).toBe(false);
    expect(win.location.assign).not.toHaveBeenCalled();
  });
});
