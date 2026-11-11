import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { EmbeddedService } from './embedded.service';
import { NativeBridgeService } from './native-bridge.service';
import { ThemeService } from './theme.service';

@Component({ template: '' })
class BlankComponent {}

describe('NativeBridgeService', () => {
  const theme = { setMode: vi.fn() };

  async function setup(embedded: boolean) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'events', component: BlankComponent },
          { path: 'events/:id', component: BlankComponent },
          { path: 'orders/personal', component: BlankComponent },
        ]),
        { provide: EmbeddedService, useValue: { isEmbedded: embedded } },
        { provide: ThemeService, useValue: theme },
      ],
    });
    const service = TestBed.inject(NativeBridgeService);
    service.install();
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/events');
    return { service, router };
  }

  afterEach(() => {
    delete window.ssskl;
    theme.setMode.mockReset();
    vi.restoreAllMocks();
  });

  it('does nothing in a normal browser', async () => {
    await setup(false);

    expect(window.ssskl).toBeUndefined();
  });

  it('navigates to app routes, replacing the current entry', async () => {
    const { router } = await setup(true);
    const navigate = vi.spyOn(router, 'navigateByUrl');

    const result = window.ssskl!.navigate('/orders/personal');
    await vi.waitFor(() => expect(router.url).toBe('/orders/personal'));

    expect(result).toBe(true);
    expect(navigate).toHaveBeenCalledWith('/orders/personal', { replaceUrl: true });
    expect(window.ssskl!.busy()).toBe(false);
  });

  it('is busy until the requested page has rendered', async () => {
    const { router } = await setup(true);
    vi.spyOn(router, 'navigateByUrl').mockReturnValue(new Promise(() => undefined));

    window.ssskl!.navigate('/orders/personal');

    expect(window.ssskl!.busy()).toBe(true);
  });

  it('is no longer busy when a navigation is refused', async () => {
    const { router } = await setup(true);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(false);

    window.ssskl!.navigate('/orders/personal');
    await Promise.resolve();
    await Promise.resolve();

    expect(window.ssskl!.busy()).toBe(false);
  });

  it('opening the current page again only resets the back stack', async () => {
    const { router } = await setup(true);
    const navigate = vi.spyOn(router, 'navigateByUrl');

    expect(window.ssskl!.navigate('/events')).toBe(true);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('refuses URLs outside the app', async () => {
    const { router } = await setup(true);
    const navigate = vi.spyOn(router, 'navigateByUrl');

    expect(window.ssskl!.navigate('https://evil.example')).toBe(false);
    expect(window.ssskl!.navigate('//evil.example/x')).toBe(false);
    expect(navigate).not.toHaveBeenCalled();
  });

  it('does not go back past the page the app opened', async () => {
    await setup(true);
    const back = vi.spyOn(window.history, 'back').mockImplementation(() => undefined);

    expect(window.ssskl!.back()).toBe(false);
    expect(back).not.toHaveBeenCalled();
  });

  it('goes back over pages opened inside the SPA', async () => {
    const { router } = await setup(true);
    const back = vi.spyOn(window.history, 'back').mockImplementation(() => undefined);

    await router.navigateByUrl('/events/1');

    expect(window.ssskl!.back()).toBe(true);
    expect(back).toHaveBeenCalledTimes(1);
  });

  it('starts counting again after the app opens a page', async () => {
    const { router } = await setup(true);
    const back = vi.spyOn(window.history, 'back').mockImplementation(() => undefined);
    await router.navigateByUrl('/events/1');

    window.ssskl!.navigate('/orders/personal');
    await vi.waitFor(() => expect(router.url).toBe('/orders/personal'));

    expect(window.ssskl!.back()).toBe(false);
    expect(back).not.toHaveBeenCalled();
  });

  it('applies valid colour modes only', async () => {
    await setup(true);

    window.ssskl!.setTheme('dark');
    window.ssskl!.setTheme('purple' as never);

    expect(theme.setMode).toHaveBeenCalledTimes(1);
    expect(theme.setMode).toHaveBeenCalledWith('dark');
  });
});
