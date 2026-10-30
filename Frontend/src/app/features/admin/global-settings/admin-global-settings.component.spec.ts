import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { MessageService } from 'primeng/api';
import AdminGlobalSettingsComponent from './admin-global-settings.component';
import { GlobalSettingsService } from '../services/global-settings.service';
import { RoleService } from '../services/role.service';

describe('AdminGlobalSettingsComponent', () => {
  let settings: Record<string, string>;
  let settingsService: {
    getSetting: ReturnType<typeof vi.fn>;
    settingExists: ReturnType<typeof vi.fn>;
    updateSetting: ReturnType<typeof vi.fn>;
    sendTestEmail: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    settings = {
      WhatsNewContent: '<p>Nieuw</p>',
      TurnstileEnabled: 'true',
      TurnstileSiteKey: 'site-key',
      TurnstileAllowedHostnames: 'ssskl.scoutingwilo.nl'
    };
    settingsService = {
      getSetting: vi.fn((key: string) => of({ key, value: settings[key] ?? '' })),
      settingExists: vi.fn((key: string) => of(key === 'TurnstileSecretKey')),
      updateSetting: vi.fn(() => of(undefined)),
      sendTestEmail: vi.fn()
    };

    TestBed.configureTestingModule({
      imports: [AdminGlobalSettingsComponent],
      providers: [
        { provide: GlobalSettingsService, useValue: settingsService },
        { provide: RoleService, useValue: { getAllRoles: vi.fn(() => of([])) } },
        { provide: MessageService, useValue: { add: vi.fn() } }
      ]
    });
  });

  function create() {
    const fixture = TestBed.createComponent(AdminGlobalSettingsComponent);
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('loads the Turnstile settings without ever reading the secret', () => {
    const component = create();
    const form = component.globalSettingsForm.getRawValue();

    expect(form.turnstileEnabled).toBe(true);
    expect(form.turnstileSiteKey).toBe('site-key');
    expect(form.turnstileAllowedHostnames).toBe('ssskl.scoutingwilo.nl');
    expect(form.turnstileSecretKey).toBe('');
    expect(component.turnstileSecretIsSet()).toBe(true);
    expect(settingsService.getSetting).not.toHaveBeenCalledWith('TurnstileSecretKey');
  });

  it('falls back to empty values for settings that are unset or fail to load', () => {
    settingsService.getSetting.mockImplementation((key: string) => {
      if (key === 'WhatsNewContent') return of(null);
      if (key === 'GoogleMapsApiKey') return throwError(() => new Error('boom'));
      return of({ key, value: settings[key] ?? '' });
    });

    const form = create().globalSettingsForm.getRawValue();

    expect(form.whatsNewContent).toBe('');
    expect(form.googleMapsApiKey).toBe('');
    expect(form.turnstileSiteKey).toBe('site-key');
  });

  it('renders the Turnstile card with the secret marked as set', () => {
    const fixture = TestBed.createComponent(AdminGlobalSettingsComponent);
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Cloudflare Turnstile');
    expect(text).toContain('Sleutel is ingesteld');
  });

  it('saves only changed settings, serialising toggles and role lists', () => {
    const component = create();
    const c = component.globalSettingsForm.controls;
    c.turnstileEnabled.setValue(false);
    c.turnstileEnabled.markAsDirty();
    c.quotesAllowedRoles.setValue(['Admin', 'User']);
    c.quotesAllowedRoles.markAsDirty();

    component.save();

    expect(settingsService.updateSetting).toHaveBeenCalledTimes(2);
    expect(settingsService.updateSetting).toHaveBeenCalledWith('TurnstileEnabled', { value: 'false' });
    expect(settingsService.updateSetting).toHaveBeenCalledWith('QuotesFeatureAllowedRoles', { value: 'Admin,User' });
    expect(component.globalSettingsForm.dirty).toBe(false);
  });

  it('keeps stored secrets when their field is left empty', () => {
    const component = create();
    const c = component.globalSettingsForm.controls;
    c.turnstileSecretKey.setValue('   ');
    c.turnstileSecretKey.markAsDirty();
    c.emailSmtpPassword.markAsDirty();

    component.save();

    expect(settingsService.updateSetting).not.toHaveBeenCalled();
    expect(component.saving()).toBe(false);
  });

  it('saves a new secret, then clears the field and marks it as set', () => {
    const component = create();
    component.turnstileSecretIsSet.set(false);
    const c = component.globalSettingsForm.controls;
    c.turnstileSecretKey.setValue('0xsecret');
    c.turnstileSecretKey.markAsDirty();

    component.save();

    expect(settingsService.updateSetting).toHaveBeenCalledWith('TurnstileSecretKey', { value: '0xsecret' });
    expect(component.turnstileSecretIsSet()).toBe(true);
    expect(c.turnstileSecretKey.value).toBe('');
  });
});
