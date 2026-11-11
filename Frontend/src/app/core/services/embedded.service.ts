import { DOCUMENT, Injectable, inject } from '@angular/core';

/**
 * What the SSSKL mobile app does natively; the SPA hands these over via a ssskl-app:// navigation.
 * `open` shows an SPA route the app has a native page for (`ssskl-app://open?path=/events/1`).
 */
export type NativeAction = 'pos' | 'profile' | 'settings' | 'logout' | 'open';

/** Set by the mobile app's document-start script. */
const APP_GLOBAL = 'SSSKL_APP';
/** Appended to the mobile app's WebView user agent (fallback when the script didn't run). */
const USER_AGENT_TOKEN = 'SSSKLApp/';

/**
 * True when the SPA runs inside the SSSKL mobile app's WebView ("embedded mode"). The app then shows
 * POS, profile, settings and the achievement / what's-new popups natively, so the SPA must not.
 * Plain function so it can be used in `app.config.ts` before DI exists.
 */
export function isEmbedded(win: Window | null | undefined = typeof window === 'undefined' ? undefined : window): boolean {
  if (!win) {
    return false;
  }
  return APP_GLOBAL in win || (win.navigator?.userAgent ?? '').includes(USER_AGENT_TOKEN);
}

@Injectable({ providedIn: 'root' })
export class EmbeddedService {
  private readonly document = inject(DOCUMENT);

  readonly isEmbedded = isEmbedded(this.document.defaultView);

  /**
   * Asks the mobile app to take over (switch to its native POS, sign out, show a route natively, ...). The
   * app intercepts the ssskl-app:// navigation and cancels it, so the page stays where it is. No-op in a
   * normal browser.
   * @param path for `open`: the app-relative route to show natively.
   */
  notifyNative(action: NativeAction, path?: string): void {
    if (this.isEmbedded) {
      const query = path ? `?path=${encodeURIComponent(path)}` : '';
      this.document.defaultView?.location.assign(`ssskl-app://${action}${query}`);
    }
  }
}
