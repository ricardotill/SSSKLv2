import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, afterNextRender, inject, input, output, signal } from '@angular/core';
import { TurnstileApi, TurnstileService } from '../../../core/services/turnstile.service';
import { LanguageService } from '../../../core/services/language.service';

/**
 * Cloudflare Turnstile widget. Emits a token once the visitor passes the check, and null when it expires
 * or fails. Tokens are single-use: call reset() after each request that consumed one.
 * Renders nothing when no sitekey is configured.
 */
@Component({
  selector: 'app-turnstile',
  template: `
    <div #container></div>
    @if (loadFailed()) {
      <small class="p-error block mt-1" role="alert">{{ lang.t().turnstile_load_failed }}</small>
    }
  `,
  host: { class: 'block mt-4' },
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TurnstileComponent {
  /** Must match the action the API checks for this endpoint (e.g. "login"). */
  readonly action = input.required<string>();
  readonly tokenChange = output<string | null>();

  protected readonly lang = inject(LanguageService);
  protected readonly loadFailed = signal(false);

  private readonly turnstile = inject(TurnstileService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private api: TurnstileApi | null = null;
  private widgetId: string | null = null;

  constructor() {
    afterNextRender(() => {
      if (this.turnstile.enabled) {
        void this.render();
      }
    });
    inject(DestroyRef).onDestroy(() => {
      if (this.api && this.widgetId) {
        this.api.remove(this.widgetId);
      }
    });
  }

  reset(): void {
    this.tokenChange.emit(null);
    if (this.api && this.widgetId) {
      this.api.reset(this.widgetId);
    }
  }

  private async render(): Promise<void> {
    try {
      this.api = await this.turnstile.load();
    } catch {
      this.loadFailed.set(true);
      return;
    }
    const container = this.host.nativeElement.querySelector('div');
    if (!container) return;
    this.widgetId = this.api.render(container, {
      sitekey: this.turnstile.siteKey,
      action: this.action(),
      theme: 'auto',
      language: 'nl',
      callback: (token) => this.tokenChange.emit(token),
      'expired-callback': () => this.tokenChange.emit(null),
      'error-callback': () => this.tokenChange.emit(null)
    }) ?? null;
  }
}
