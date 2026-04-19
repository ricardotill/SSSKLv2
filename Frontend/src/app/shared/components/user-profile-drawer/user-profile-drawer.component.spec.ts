import { TestBed, ComponentFixture } from '@angular/core/testing';
import { UserProfileDrawerComponent } from './user-profile-drawer.component';
import { UserProfileDrawerService } from '../../../core/services/user-profile-drawer.service';
import { AuthService } from '../../../core/auth/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { HttpClient } from '@angular/common/http';
import { MessageService, ConfirmationService } from 'primeng/api';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { signal } from '@angular/core';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { vi } from 'vitest';
import { ApplicationUserDto } from '../../../core/models/application-user.model';
import { AchievementService } from '../../../features/achievements/services/achievement.service';

describe('UserProfileDrawerComponent', () => {
  let component: UserProfileDrawerComponent;
  let fixture: ComponentFixture<UserProfileDrawerComponent>;
  let mockDrawerService: any;
  let mockAuthService: any;
  let mockLanguageService: any;
  let mockHttp: any;
  let mockMessageService: any;
  let mockConfirmationService: any;
  let mockRouter: any;
  let mockAchievementService: any;

  const mockUser: ApplicationUserDto = {
    id: 'user-123',
    userName: 'testuser',
    fullName: 'Test User',
    saldo: 10,
    lastOrdered: new Date().toISOString(),
    profilePictureUrl: 'path/to/pic.jpg',
    roles: [],
    description: 'Old Description'
  };

  beforeEach(async () => {
    mockDrawerService = {
      drawerVisible: signal(false),
      selectedUserId: signal<string | null>(null),
      close: vi.fn()
    };

    mockAuthService = {
      currentUser: signal({ id: 'me', userName: 'me' }),
      updateMe: vi.fn().mockReturnValue(of({})),
      uploadProfilePicture: vi.fn().mockReturnValue(of({})),
      deleteProfilePicture: vi.fn().mockReturnValue(of({}))
    };

    mockLanguageService = {
      t: signal({
        profile: 'Profile',
        change_picture: 'Change Picture',
        user_settings: 'Settings',
        description: 'About Me',
        success: 'Success',
        error: 'Error',
        delete: 'Delete',
        achievements: 'Achievements',
        no_achievements: 'No achievements yet'
      })
    };

    mockHttp = {
      get: vi.fn().mockReturnValue(of(mockUser))
    };

    mockMessageService = {
      add: vi.fn()
    };

    mockConfirmationService = {
      confirm: vi.fn().mockImplementation((config) => config.accept())
    };

    mockRouter = {
      navigate: vi.fn()
    };

    mockAchievementService = {
      getAchievementEntries: vi.fn().mockReturnValue(of([]))
    };

    await TestBed.configureTestingModule({
      imports: [UserProfileDrawerComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: UserProfileDrawerService, useValue: mockDrawerService },
        { provide: AuthService, useValue: mockAuthService },
        { provide: LanguageService, useValue: mockLanguageService },
        { provide: HttpClient, useValue: mockHttp },
        { provide: MessageService, useValue: mockMessageService },
        { provide: ConfirmationService, useValue: mockConfirmationService },
        { provide: Router, useValue: mockRouter },
        { provide: AchievementService, useValue: mockAchievementService }
      ]
    })
    // No need for override if we provide it in TestBed
    .compileComponents();

    fixture = TestBed.createComponent(UserProfileDrawerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not show drawer by default', () => {
    expect(mockDrawerService.drawerVisible()).toBe(false);
  });

  it('should load user data when selectedUserId changes', async () => {
    mockDrawerService.selectedUserId.set('user-123');
    // effect runs asynchronously, we might need to wait or trigger it
    // In many Vitest/Angular setups, detectChanges or flush is needed.
    fixture.detectChanges();
    
    expect(mockHttp.get).toHaveBeenCalledWith(expect.stringContaining('/api/v1/ApplicationUser/user-123/profile'));
    expect(component.user()).toEqual(mockUser);
    expect(component.editDescription).toBe(mockUser.description);
  });

  it('should correctly identify current user', () => {
    // Set current user same as mockUser.userName
    mockAuthService.currentUser.set({ id: 'user-123', userName: 'testuser' });
    component.user.set(mockUser);
    expect(component.isCurrentUser()).toBe(true);

    mockAuthService.currentUser.set({ id: 'other', userName: 'other' });
    expect(component.isCurrentUser()).toBe(false);
  });

  it('should start and cancel edit mode', () => {
    mockAuthService.currentUser.set({ id: 'user-123', userName: 'testuser' });
    component.user.set(mockUser);
    
    component.startEdit();
    expect(component.editMode()).toBe(true);
    expect(component.editDescription).toBe(mockUser.description);

    component.cancelEdit();
    expect(component.editMode()).toBe(false);
  });

  it('should save description successfully', () => {
    mockAuthService.currentUser.set({ id: 'user-123', userName: 'testuser' });
    component.user.set(mockUser);
    component.editDescription = 'New Description';
    component.editMode.set(true);

    component.saveDescription();

    expect(mockAuthService.updateMe).toHaveBeenCalledWith({ description: 'New Description' });
    expect(component.user()?.description).toBe('New Description');
    expect(component.editMode()).toBe(false);
    expect(mockMessageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('should handle save error', () => {
    mockAuthService.currentUser.set({ id: 'user-123', userName: 'testuser' });
    component.user.set(mockUser);
    mockAuthService.updateMe.mockReturnValue(throwError(() => new Error('API Error')));

    component.saveDescription();

    expect(component.saving()).toBe(false);
    expect(mockMessageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('should navigate to settings and close drawer', () => {
    component.goToSettings();
    expect(mockDrawerService.close).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/settings']);
  });

  it('should delete profile picture with confirmation', () => {
    mockDrawerService.selectedUserId.set('user-123');
    component.onDeleteProfilePicture();

    expect(mockConfirmationService.confirm).toHaveBeenCalled();
    expect(mockAuthService.deleteProfilePicture).toHaveBeenCalled();
    expect(mockMessageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });
});
