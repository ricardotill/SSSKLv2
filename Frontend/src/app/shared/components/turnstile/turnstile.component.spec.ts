import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { environment } from '../../../../environments/environment';
import { TurnstileComponent } from './turnstile.component';
import { TurnstileRenderOptions } from '../../../core/services/turnstile.service';

describe('TurnstileComponent', () => {
  let originalSiteKey: string;
  let renderOptions: TurnstileRenderOptions | undefined;
  let api: { render: ReturnType<typeof vi.fn>; reset: ReturnType<typeof vi.fn>; remove: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    originalSiteKey = environment.turnstileSiteKey;
    (environment as any).turnstileSiteKey = 'site-key';
    api = {
      render: vi.fn((_el: HTMLElement, options: TurnstileRenderOptions) => {
        renderOptions = options;
        return 'widget-1';
      }),
      reset: vi.fn(),
      remove: vi.fn()
    };
    (window as any).turnstile = api;
  });

  afterEach(() => {
    (environment as any).turnstileSiteKey = originalSiteKey;
    delete (window as any).turnstile;
    renderOptions = undefined;
  });

  async function create() {
    const fixture = TestBed.createComponent(TurnstileComponent);
    fixture.componentRef.setInput('action', 'login');
    const tokens: (string | null)[] = [];
    fixture.componentInstance.tokenChange.subscribe((t) => tokens.push(t));
    fixture.detectChanges();
    await fixture.whenStable();
    await Promise.resolve();
    return { fixture, tokens };
  }

  it('renders the widget with the sitekey and action and emits tokens', async () => {
    const { tokens } = await create();

    expect(api.render).toHaveBeenCalledTimes(1);
    expect(renderOptions?.sitekey).toBe('site-key');
    expect(renderOptions?.action).toBe('login');

    renderOptions?.callback?.('tok');
    renderOptions?.['expired-callback']?.();
    expect(tokens).toEqual(['tok', null]);
  });

  it('reset clears the token and resets the widget', async () => {
    const { fixture, tokens } = await create();

    fixture.componentInstance.reset();

    expect(tokens).toEqual([null]);
    expect(api.reset).toHaveBeenCalledWith('widget-1');
  });

  it('removes the widget on destroy', async () => {
    const { fixture } = await create();

    fixture.destroy();

    expect(api.remove).toHaveBeenCalledWith('widget-1');
  });
});
