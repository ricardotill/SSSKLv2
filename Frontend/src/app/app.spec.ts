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
        { provide: PrimeNG, useValue: { setTranslation: () => {} } },
        { provide: ThemeService, useValue: { init: () => {} } },
        { provide: AuthService, useValue: mockAuthService },
        { provide: AchievementPopupService, useValue: mockPopupService },
        { provide: UserProfileDrawerService, useValue: mockDrawerService },
        { provide: MessageService, useValue: {} },
        { provide: ConfirmationService, useValue: {} }
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
