import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';
import SettingsComponent from './settings.component';
import { AuthService } from '../../core/auth/auth.service';
import { WebAuthnService } from '../auth/services/webauthn.service';
import { ThemeService } from '../../core/services/theme.service';
import { PushNotificationService } from '../../core/services/push-notification.service';
import { EmbeddedService } from '../../core/services/embedded.service';

describe('SettingsComponent (render)', () => {
  // PrimeNG's tab list measures itself; jsdom has no ResizeObserver.
  beforeAll(() => {
    globalThis.ResizeObserver ??= class {
      observe() {}
      unobserve() {}
      disconnect() {}
    } as unknown as typeof ResizeObserver;
  });

  function render(options: { embedded: boolean; tab?: string }) {
    TestBed.configureTestingModule({
      imports: [SettingsComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        MessageService,
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(options.tab ? { tab: options.tab } : {}) } },
        },
        {
          provide: AuthService,
          useValue: {
            currentUser: signal({ id: 'u1', name: 'Jan', surname: 'Jansen', roles: ['User'] }),
            getIdentityInfo: () => of({ email: 'jan@example.com', isEmailConfirmed: true }),
            manage2fa: () => of({ isTwoFactorEnabled: false, sharedKey: 'KEY', recoveryCodesLeft: 0 }),
            getPasskeys: () => of([]),
          },
        },
        { provide: WebAuthnService, useValue: {} },
        { provide: ThemeService, useValue: { mode: signal('auto'), setMode: () => undefined } },
        {
          provide: PushNotificationService,
          useValue: { isSupported: signal(false), isEnabled: signal(false) },
        },
        { provide: EmbeddedService, useValue: { isEmbedded: options.embedded } },
      ],
    });
    const fixture = TestBed.createComponent(SettingsComponent);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows all tabs and the colour mode in a browser', () => {
    const root = render({ embedded: false });

    expect(root.querySelector('h1')?.textContent).toContain('Gebruikersinstellingen');
    expect(root.querySelectorAll('p-tab').length).toBe(4);
    expect(root.querySelector('p-selectbutton')).not.toBeNull();
  });

  it('opens only the requested tab in the mobile app', () => {
    const root = render({ embedded: true, tab: 'security' });

    expect(root.querySelector('h1')?.textContent).toContain('Accountbeveiliging');
    expect(root.querySelector('p-tablist')).toBeNull();
    expect(root.querySelector('#newEmail')).not.toBeNull();
  });

  it('falls back to the profile tab, without colour mode or push, in the mobile app', () => {
    const root = render({ embedded: true, tab: 'unknown' });

    expect(root.querySelector('h1')?.textContent).toContain('Profiel');
    expect(root.querySelector('p-selectbutton')).toBeNull();
    expect(root.querySelector('p-toggleswitch')).toBeNull();
  });
});
