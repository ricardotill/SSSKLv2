import { DOCUMENT, Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

/** Minimal typing for the parts of the Cloudflare Turnstile API we use. */
export interface TurnstileApi {
  render(container: HTMLElement, options: TurnstileRenderOptions): string | undefined;
  reset(widgetId: string): void;
  remove(widgetId: string): void;
}

export interface TurnstileRenderOptions {
  sitekey: string;
  action: string;
  theme?: 'auto' | 'light' | 'dark';
  language?: string;
  callback?: (token: string) => void;
  'expired-callback'?: () => void;
  'error-callback'?: () => void;
}

const SCRIPT_URL = 'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';

export interface TurnstileConfig {
  enabled: boolean;
  siteKey: string | null;
}

/** Header the API reads the Turnstile token from (see TurnstileEndpointFilter). */
export const TURNSTILE_HEADER = 'X-Turnstile-Token';

@Injectable({ providedIn: 'root' })
export class TurnstileService {
  private readonly document = inject(DOCUMENT);
  private readonly http = inject(HttpClient);
  private scriptPromise: Promise<TurnstileApi> | null = null;
  private configRequested = false;

  /** Set by admins in Global Settings; fetched once per app load. Null until it arrives. */
  readonly config = signal<TurnstileConfig | null>(null);
  readonly enabled = computed(() => !!this.config()?.enabled && !!this.config()?.siteKey);
  readonly siteKey = computed(() => this.config()?.siteKey ?? '');

  /** Fetches the public Turnstile settings once. A failed fetch leaves Turnstile off; the API still decides. */
  loadConfig(): void {
    if (this.configRequested) return;
    this.configRequested = true;
    this.http.get<TurnstileConfig>('/api/v1/GlobalSettings/public/turnstile').subscribe({
      next: (config) => this.config.set(config),
      error: () => {
        this.configRequested = false;
        this.config.set({ enabled: false, siteKey: null });
      }
    });
  }

  /** Loads the Turnstile script once and resolves with the global API. */
  load(): Promise<TurnstileApi> {
    const win = this.document.defaultView as (Window & { turnstile?: TurnstileApi }) | null;
    if (win?.turnstile) {
      return Promise.resolve(win.turnstile);
    }
    this.scriptPromise ??= new Promise<TurnstileApi>((resolve, reject) => {
      const script = this.document.createElement('script');
      script.src = SCRIPT_URL;
      script.async = true;
      script.defer = true;
      script.onload = () => (win?.turnstile ? resolve(win.turnstile) : reject(new Error('Turnstile did not initialise')));
      script.onerror = () => {
        this.scriptPromise = null;
        reject(new Error('Turnstile script failed to load'));
      };
      this.document.head.appendChild(script);
    });
    return this.scriptPromise;
  }

  /** True while a gated form must wait for the visitor to pass the check. */
  blocksSubmit(token: string | null): boolean {
    return this.enabled() && !token;
  }

  /** Headers carrying the token for a gated request; empty when Turnstile is disabled. */
  headers(token: string | null): HttpHeaders {
    return this.enabled() && token ? new HttpHeaders({ [TURNSTILE_HEADER]: token }) : new HttpHeaders();
  }
}

/** True when the API rejected a request because the Turnstile check failed. */
export function isTurnstileFailure(error: unknown): boolean {
  const err = error as { status?: number; error?: { code?: string } } | null;
  return err?.status === 403 && err.error?.code === 'turnstile_failed';
}
