import { DOCUMENT, Injectable, inject } from '@angular/core';

const LOCKED_VIEWPORT = 'width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no';
const ZOOM_KEYS = new Set(['+', '=', '-', '_', '0']);

/**
 * Makes the installed PWA behave like a native app by disabling zoom.
 *
 * Only active when running as an installed app (display-mode standalone/fullscreen, or iOS
 * home-screen mode). In a normal browser tab zoom stays available, because blocking it there
 * fails WCAG 1.4.4 (Resize Text) and the AXE "meta-viewport" check.
 */
@Injectable({ providedIn: 'root' })
export class PwaZoomLockService {
  private readonly document = inject(DOCUMENT);
  private active = false;

  get isActive(): boolean {
    return this.active;
  }

  init(): void {
    const win = this.document.defaultView;
    if (this.active || !win || !this.isInstalledApp(win)) {
      return;
    }
    this.active = true;

    this.document.documentElement.classList.add('pwa-standalone');
    this.document.querySelector('meta[name="viewport"]')?.setAttribute('content', LOCKED_VIEWPORT);

    const prevent = (e: Event) => e.preventDefault();
    const blocking = { passive: false } as const;

    // iOS ignores user-scalable=no; its pinch gestures can only be cancelled via these WebKit events.
    for (const type of ['gesturestart', 'gesturechange', 'gestureend']) {
      this.document.addEventListener(type, prevent, blocking);
    }
    // Any other multi-touch pinch.
    this.document.addEventListener('touchmove', (e: Event) => {
      if ((e as TouchEvent).touches?.length > 1) {
        e.preventDefault();
      }
    }, blocking);

    // Installed desktop app: trackpad pinch / Ctrl+scroll and Ctrl/Cmd + '+', '-', '0'.
    win.addEventListener('wheel', (e: WheelEvent) => {
      if (e.ctrlKey || e.metaKey) {
        e.preventDefault();
      }
    }, blocking);
    win.addEventListener('keydown', (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && ZOOM_KEYS.has(e.key)) {
        e.preventDefault();
      }
    });
  }

  private isInstalledApp(win: Window): boolean {
    const displayMode = (mode: string) => win.matchMedia?.(`(display-mode: ${mode})`).matches ?? false;
    const iosStandalone = (win.navigator as Navigator & { standalone?: boolean }).standalone === true;
    return displayMode('standalone') || displayMode('fullscreen') || iosStandalone;
  }
}
