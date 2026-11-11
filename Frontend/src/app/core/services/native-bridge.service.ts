import { DOCUMENT, DestroyRef, Injectable, inject } from '@angular/core';
import { NavigationEnd, NavigationStart, Router } from '@angular/router';
import { EmbeddedService } from './embedded.service';
import { ThemeMode, ThemeService } from './theme.service';

/**
 * What the SSSKL mobile app calls on `window.ssskl` (with EvaluateJavaScript). The app keeps one WebView
 * alive and opens SPA pages through `navigate`, so switching pages doesn't reload the SPA.
 */
export interface NativeBridge {
  /** Opens an SPA route, replacing the current history entry. Returns false for a URL the SPA won't open. */
  navigate(url: string): boolean;
  /** Goes back one page if the user navigated within the SPA since the app last opened a page. */
  back(): boolean;
  /** Applies the app's colour mode. */
  setTheme(mode: ThemeMode): void;
  /** True until the first page, or the page navigate() asked for, has rendered. The app polls it to fade in. */
  busy(): boolean;
}

declare global {
  interface Window {
    ssskl?: NativeBridge;
  }
}

const THEME_MODES: readonly ThemeMode[] = ['auto', 'light', 'dark'];

@Injectable({ providedIn: 'root' })
export class NativeBridgeService {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly theme = inject(ThemeService);
  private readonly embedded = inject(EmbeddedService).isEmbedded;
  private readonly destroyRef = inject(DestroyRef);

  /** Pages the user opened inside the SPA since the app last called navigate(); back() never goes further. */
  private depth = 0;
  private resetOnNextNavigation = false;
  private initialNavigationDone = false;
  private lastTrigger: NavigationStart['navigationTrigger'];

  /** Exposes `window.ssskl` in the mobile app. No-op in a normal browser. */
  install(): void {
    const win = this.document.defaultView;
    if (!this.embedded || !win) {
      return;
    }

    const subscription = this.router.events.subscribe((event) => {
      if (event instanceof NavigationStart) {
        this.lastTrigger = event.navigationTrigger;
      } else if (event instanceof NavigationEnd) {
        this.onNavigationEnd();
      }
    });
    this.destroyRef.onDestroy(() => {
      subscription.unsubscribe();
      delete win.ssskl;
    });

    win.ssskl = {
      navigate: (url) => this.navigate(url),
      back: () => this.back(),
      busy: () => this.resetOnNextNavigation || !this.initialNavigationDone,
      setTheme: (mode) => {
        if (THEME_MODES.includes(mode)) {
          this.theme.setMode(mode);
        }
      },
    };
  }

  private onNavigationEnd(): void {
    if (this.resetOnNextNavigation || !this.initialNavigationDone) {
      // The page the app opened (by URL or navigate()) is the bottom of the stack.
      this.resetOnNextNavigation = false;
      this.initialNavigationDone = true;
      this.depth = 0;
    } else if (this.lastTrigger === 'popstate') {
      this.depth = Math.max(0, this.depth - 1);
    } else {
      this.depth++;
    }
  }

  private navigate(url: string): boolean {
    // Only app-relative routes: never let the bridge open another origin.
    if (typeof url !== 'string' || !url.startsWith('/') || url.startsWith('//')) {
      return false;
    }
    if (this.router.url === url) {
      this.depth = 0;
      return true;
    }
    this.resetOnNextNavigation = true;
    this.router.navigateByUrl(url, { replaceUrl: true }).then(
      (ok) => {
        if (!ok) {
          this.resetOnNextNavigation = false;
        }
      },
      () => (this.resetOnNextNavigation = false),
    );
    return true;
  }

  private back(): boolean {
    if (this.depth === 0) {
      return false;
    }
    this.document.defaultView?.history.back();
    return true;
  }
}
