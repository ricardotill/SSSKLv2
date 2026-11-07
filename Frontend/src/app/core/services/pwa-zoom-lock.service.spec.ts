import { TestBed } from '@angular/core/testing';
import { DOCUMENT } from '@angular/core';
import { vi } from 'vitest';
import { PwaZoomLockService } from './pwa-zoom-lock.service';

describe('PwaZoomLockService', () => {
  const originalViewport = 'width=device-width, initial-scale=1';
  let doc: Document;
  let meta: HTMLMetaElement;
  let service: PwaZoomLockService;

  function setStandalone(standalone: boolean) {
    vi.stubGlobal('matchMedia', (query: string) => ({
      matches: standalone && query === '(display-mode: standalone)',
      media: query,
    }));
  }

  function cancelable(type: string, init: Record<string, unknown> = {}): Event {
    const event = new Event(type, { cancelable: true, bubbles: true });
    Object.entries(init).forEach(([k, v]) => Object.defineProperty(event, k, { value: v }));
    return event;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({});
    doc = TestBed.inject(DOCUMENT);
    meta = doc.createElement('meta');
    meta.name = 'viewport';
    meta.content = originalViewport;
    doc.head.appendChild(meta);
    service = TestBed.inject(PwaZoomLockService);
  });

  afterEach(() => {
    meta.remove();
    doc.documentElement.classList.remove('pwa-standalone');
    vi.unstubAllGlobals();
  });

  it('leaves zoom alone in a normal browser tab', () => {
    setStandalone(false);
    service.init();

    expect(service.isActive).toBe(false);
    expect(meta.content).toBe(originalViewport);
    expect(doc.documentElement.classList.contains('pwa-standalone')).toBe(false);
  });

  describe('when running as an installed app', () => {
    beforeEach(() => {
      setStandalone(true);
      service.init();
    });

    it('locks the viewport scale', () => {
      expect(service.isActive).toBe(true);
      expect(meta.content).toContain('maximum-scale=1');
      expect(meta.content).toContain('user-scalable=no');
      expect(doc.documentElement.classList.contains('pwa-standalone')).toBe(true);
    });

    it('cancels iOS pinch gestures', () => {
      const event = cancelable('gesturestart');
      doc.dispatchEvent(event);
      expect(event.defaultPrevented).toBe(true);
    });

    it('cancels multi-touch moves but not single-finger scrolling', () => {
      const pinch = cancelable('touchmove', { touches: { length: 2 } });
      const scroll = cancelable('touchmove', { touches: { length: 1 } });
      doc.dispatchEvent(pinch);
      doc.dispatchEvent(scroll);
      expect(pinch.defaultPrevented).toBe(true);
      expect(scroll.defaultPrevented).toBe(false);
    });

    it('cancels ctrl+wheel and keyboard zoom, but not normal scrolling or typing', () => {
      const ctrlWheel = cancelable('wheel', { ctrlKey: true });
      const wheel = cancelable('wheel', { ctrlKey: false });
      const ctrlPlus = cancelable('keydown', { ctrlKey: true, key: '+' });
      const plain = cancelable('keydown', { ctrlKey: false, key: '+' });
      for (const e of [ctrlWheel, wheel, ctrlPlus, plain]) {
        window.dispatchEvent(e);
      }
      expect(ctrlWheel.defaultPrevented).toBe(true);
      expect(wheel.defaultPrevented).toBe(false);
      expect(ctrlPlus.defaultPrevented).toBe(true);
      expect(plain.defaultPrevented).toBe(false);
    });

    it('only installs its listeners once', () => {
      const spy = vi.spyOn(doc, 'addEventListener');
      service.init();
      expect(spy).not.toHaveBeenCalled();
    });
  });
});
