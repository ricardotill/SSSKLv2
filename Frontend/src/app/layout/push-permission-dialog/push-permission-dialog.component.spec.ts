import { TestBed } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { SwPush } from '@angular/service-worker';
import { signal } from '@angular/core';
import { vi } from 'vitest';
import { PushPermissionDialogComponent } from './push-permission-dialog.component';
import { PushNotificationService } from '../../core/services/push-notification.service';
import { LanguageService } from '../../core/services/language.service';

// ── Minimal LanguageService mock ───────────────────────────────────────────────

const langMock = {
  t: () => ({
    'PUSH.DIALOG.TITLE':       'Blijf op de hoogte!',
    'PUSH.DIALOG.HEADING':     'Push Notificaties',
    'PUSH.DIALOG.DESCRIPTION': 'Testomschrijving',
    'PUSH.DIALOG.ENABLE':      'Inschakelen',
    'PUSH.DIALOG.LATER':       'Misschien later',
  }),
};

// ── Push service mock ──────────────────────────────────────────────────────────

function makePushServiceMock() {
  return {
    isSupported:  signal(true),
    isEnabled:    signal(false),
    showPrompt:   signal(true),
    subscribe:    vi.fn().mockResolvedValue(undefined),
    unsubscribe:  vi.fn().mockResolvedValue(undefined),
    setPrompted:  vi.fn(),
  };
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe('PushPermissionDialogComponent', () => {
  let fixture: ComponentFixture<PushPermissionDialogComponent>;
  let component: PushPermissionDialogComponent;
  let pushServiceMock: ReturnType<typeof makePushServiceMock>;

  beforeEach(() => {
    pushServiceMock = makePushServiceMock();
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      imports: [PushPermissionDialogComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PushNotificationService, useValue: pushServiceMock },
        { provide: LanguageService, useValue: langMock },
        {
          provide: SwPush,
          useValue: {
            isEnabled: true,
            subscription: { subscribe: vi.fn() },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(PushPermissionDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── Initialization ─────────────────────────────────────────────────────────

  it('should be created', () => {
    expect(component).toBeTruthy();
  });

  it('should set visible to true on initialization', () => {
    expect(component.visible).toBe(true);
  });

  it('loading signal should be false initially', () => {
    expect(component.loading()).toBe(false);
  });

  // ── enable() ──────────────────────────────────────────────────────────────

  it('enable() should call pushService.subscribe', async () => {
    await component.enable();
    expect(pushServiceMock.subscribe).toHaveBeenCalledOnce();
  });

  it('enable() should close the dialog on success', async () => {
    await component.enable();
    expect(component.visible).toBe(false);
  });

  it('enable() should call setPrompted on success', async () => {
    await component.enable();
    expect(pushServiceMock.setPrompted).toHaveBeenCalledOnce();
  });

  it('enable() should keep dialog open on error', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    pushServiceMock.subscribe.mockRejectedValue(new Error('Permission denied'));
    await component.enable();
    // Dialog stays open so user can dismiss manually
    expect(component.visible).toBe(true);
  });

  it('enable() should reset loading to false after error', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    pushServiceMock.subscribe.mockRejectedValue(new Error('Denied'));
    await component.enable();
    expect(component.loading()).toBe(false);
  });

  // ── close() ───────────────────────────────────────────────────────────────

  it('close() should set visible to false', () => {
    component.close();
    expect(component.visible).toBe(false);
  });

  it('close() should call setPrompted', () => {
    component.close();
    expect(pushServiceMock.setPrompted).toHaveBeenCalledOnce();
  });
});
