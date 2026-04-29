import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { App } from './app';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { AuthService } from './core/auth/auth.service';
import { AchievementPopupService } from './features/achievements/services/achievement-popup.service';
import { UserProfileDrawerService } from './core/services/user-profile-drawer.service';
import { MessageService, ConfirmationService } from 'primeng/api';
import { provideRouter } from '@angular/router';
import { PrimeNG } from 'primeng/config';
import { ThemeService } from './core/services/theme.service';
import { PushNotificationService } from './core/services/push-notification.service';
import { SwPush } from '@angular/service-worker';
import { vi } from 'vitest';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { AppUpdateService } from './core/services/app-update.service';

describe('App', () => {
  beforeEach(async () => {
    const mockAuthService = { 
      currentUser: signal(null),
      isInitialized: signal(true)
    };
    const mockPopupService = { 
      checkUnseenAchievements: () => {},
      unseenEntries: signal([])
    };
    const mockDrawerService = { 
      drawerVisible: signal(false), 
      selectedUserId: signal(null) 
    };

    await TestBed.configureTestingModule({
      imports: [App, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        providePrimeNG({ theme: { preset: Aura } }),
        { provide: ThemeService, useValue: { init: () => {} } },
        { provide: AuthService, useValue: mockAuthService },
        { provide: AchievementPopupService, useValue: mockPopupService },
        { provide: UserProfileDrawerService, useValue: mockDrawerService },
        { provide: MessageService, useValue: {} },
        { provide: ConfirmationService, useValue: {} },
        {
          provide: SwPush,
          useValue: { isEnabled: false, subscription: { subscribe: vi.fn() } }
        },
        {
          provide: PushNotificationService,
          useValue: { showPrompt: signal(false), isSupported: signal(false), isEnabled: signal(false) }
        },
        { provide: AppUpdateService, useValue: {} }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render router-outlet', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });
});
