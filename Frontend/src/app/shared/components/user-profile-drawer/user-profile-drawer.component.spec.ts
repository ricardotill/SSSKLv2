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
import { QuoteService } from '../../../features/quotes/services/quote.service';
import { ApplicationUserService } from '../../../features/users/services/application-user.service';

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
  let mockQuoteService: any;

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

    mockQuoteService = {
      getQuotes: vi.fn().mockReturnValue(of({ items: [], totalCount: 0 }))
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
        { provide: AchievementService, useValue: mockAchievementService },
        { provide: QuoteService, useValue: mockQuoteService }
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

  it('should let an admin navigate to another user\'s stats', () => {
    mockAuthService.currentUser.set({ id: 'admin', userName: 'admin', roles: ['Admin'] });
    component.user.set(mockUser);

    component.goToStats();

    expect(mockDrawerService.close).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/stats'], { queryParams: { userId: mockUser.id } });
  });

  it('should delete profile picture with confirmation', () => {
    mockDrawerService.selectedUserId.set('user-123');
    component.onDeleteProfilePicture();

    expect(mockConfirmationService.confirm).toHaveBeenCalled();
    expect(mockAuthService.deleteProfilePicture).toHaveBeenCalled();
    expect(mockMessageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  describe('rendered drawer', () => {
    const achievements = [
      { id: 'e1', achievementId: 'a1', achievementName: 'Old', tier: 'Bronze', imageUrl: null, dateAdded: '2024-01-01' },
      { id: 'e2', achievementId: 'a2', achievementName: 'New', tier: 'Gold', imageUrl: 'img.png', dateAdded: '2025-01-01' },
    ];
    const quotes = [{ id: 'q1', text: 'Hello', dateSaid: '2025-01-01', voteCount: 2, commentsCount: 1 }];

    function open(currentUser: any, opts: { achievements?: any[]; quotes?: any[]; user?: any } = {}) {
      mockAuthService.currentUser.set(currentUser);
      mockHttp.get.mockReturnValue(of(opts.user ?? mockUser));
      mockAchievementService.getAchievementEntries.mockReturnValue(of(opts.achievements ?? achievements));
      mockQuoteService.getQuotes.mockReturnValue(of({ items: opts.quotes ?? quotes, totalCount: 1 }));
      mockDrawerService.drawerVisible.set(true);
      mockDrawerService.selectedUserId.set('user-123');
      fixture.detectChanges();
    }

    afterEach(() => {
      mockDrawerService.drawerVisible.set(false);
      fixture.detectChanges();
    });

    it('renders the current user profile with achievements and quotes', () => {
      open({ id: 'user-123', userName: 'testuser', roles: [] });

      expect(component.achievements().map(a => a.id)).toEqual(['e2', 'e1']);
      expect(component.quotes().length).toBe(1);
      expect(document.body.textContent).toContain('Test User');
      expect(document.body.textContent).toContain('Hello');
    });

    it('renders the edit mode and empty states', () => {
      open({ id: 'user-123', userName: 'testuser', roles: [] }, {
        achievements: [],
        quotes: [],
        user: { ...mockUser, description: '', profilePictureUrl: null },
      });
      component.startEdit();
      fixture.detectChanges();

      expect(component.editMode()).toBe(true);
      expect(document.body.textContent).toContain('Geen quotes gevonden.');
    });

    it('renders another user as admin with a stats recalculation button', () => {
      open({ id: 'admin', userName: 'admin', roles: ['Admin'] });

      expect(component.isAdmin()).toBe(true);
      expect(component.isCurrentUser()).toBe(false);
      expect(document.body.textContent).toContain('Statistieken herberekenen');
    });

    it('shows a not-found message when the user fails to load', () => {
      mockHttp.get.mockReturnValue(throwError(() => new Error('nope')));
      mockDrawerService.drawerVisible.set(true);
      mockDrawerService.selectedUserId.set('missing');
      fixture.detectChanges();

      expect(component.user()).toBeNull();
      expect(document.body.textContent).toContain('Gebruiker niet gevonden.');
    });
  });

  it('hides the quotes section when quotes are forbidden', () => {
    mockQuoteService.getQuotes.mockReturnValue(throwError(() => ({ status: 403 })));
    mockAchievementService.getAchievementEntries.mockReturnValue(throwError(() => new Error('x')));
    component.loadUser('user-123');

    expect(component.hasQuoteAccess()).toBe(false);
    expect(component.quotes()).toEqual([]);
    expect(component.achievements()).toEqual([]);
  });

  it('resets state when the selected user is cleared', () => {
    mockDrawerService.selectedUserId.set('user-123');
    fixture.detectChanges();
    mockDrawerService.selectedUserId.set(null);
    fixture.detectChanges();

    expect(component.user()).toBeNull();
    expect(component.hasQuoteAccess()).toBe(true);
  });

  it('recalculates stats as admin', () => {
    const appUserService = TestBed.inject(ApplicationUserService);
    const stats = { totalOrders: 3, currentStreak: 1 } as any;
    vi.spyOn(appUserService, 'recalculateUserStats').mockReturnValue(of(stats));
    mockAuthService.currentUser.set({ id: 'admin', userName: 'admin', roles: ['Admin'] });
    component.user.set(mockUser);

    component.recalculateStats();

    expect(component.stats()).toEqual(stats);
    expect(component.recalculatingStats()).toBe(false);
    expect(mockMessageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('reports a failed stats recalculation', () => {
    const appUserService = TestBed.inject(ApplicationUserService);
    vi.spyOn(appUserService, 'recalculateUserStats').mockReturnValue(throwError(() => new Error('x')));
    mockAuthService.currentUser.set({ id: 'admin', userName: 'admin', roles: ['Admin'] });
    component.user.set(mockUser);

    component.recalculateStats();

    expect(component.recalculatingStats()).toBe(false);
    expect(mockMessageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('does not recalculate stats for non-admins', () => {
    const appUserService = TestBed.inject(ApplicationUserService);
    const spy = vi.spyOn(appUserService, 'recalculateUserStats');
    component.user.set(mockUser);

    component.recalculateStats();

    expect(spy).not.toHaveBeenCalled();
  });

  it('navigates to own stats', () => {
    mockAuthService.currentUser.set({ id: 'user-123', userName: 'testuser' });
    component.user.set(mockUser);

    component.goToStats();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/stats']);
  });

  it('does not navigate to stats for other users without admin', () => {
    component.user.set(mockUser);
    component.goToStats();
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('navigates to achievements and quotes', () => {
    component.user.set(mockUser);

    component.navigateToAchievements();
    component.navigateToAchievementDetail('a1');
    component.navigateToQuotes();
    component.navigateToQuoteDetail('q1');

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/achievements'], { queryParams: { userId: 'user-123' } });
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/achievements', 'a1']);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/quotes'], { queryParams: { userId: 'user-123' } });
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/quotes', 'q1']);
    expect(mockDrawerService.close).toHaveBeenCalledTimes(4);
  });

  it('closes when the drawer is hidden', () => {
    component.onVisibleChange(true);
    expect(mockDrawerService.close).not.toHaveBeenCalled();
    component.onVisibleChange(false);
    expect(mockDrawerService.close).toHaveBeenCalled();
  });

  it('closes on a click outside the drawer', () => {
    component.onDocumentClick({ target: document.body } as any);
    expect(mockDrawerService.close).not.toHaveBeenCalled();

    mockDrawerService.drawerVisible.set(true);
    component.onDocumentClick({ target: document.body } as any);
    expect(mockDrawerService.close).toHaveBeenCalled();
  });

  it('stays open on a click inside the drawer', () => {
    mockDrawerService.drawerVisible.set(true);
    const inside = document.createElement('div');
    inside.className = 'user-profile-drawer';
    const child = document.createElement('span');
    inside.appendChild(child);
    document.body.appendChild(inside);

    component.onDocumentClick({ target: child } as any);

    expect(mockDrawerService.close).not.toHaveBeenCalled();
    inside.remove();
  });

  it('ignores edits for other users', () => {
    component.user.set(mockUser);
    component.startEdit();
    component.saveDescription();
    expect(component.editMode()).toBe(false);
    expect(mockAuthService.updateMe).not.toHaveBeenCalled();
  });

  it('uploads a selected profile picture and reloads the user', () => {
    mockDrawerService.selectedUserId.set('user-123');
    component.fileInput = { nativeElement: { value: 'x' } };
    const file = new File(['x'], 'pic.png', { type: 'image/png' });

    component.fileChangeEvent({ target: { files: [file] } });

    expect(mockAuthService.uploadProfilePicture).toHaveBeenCalledWith('', file);
    expect(component.fileInput.nativeElement.value).toBe('');
    expect(component.isUploadingPicture()).toBe(false);
    expect(mockMessageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('reports a failed profile picture upload', () => {
    mockAuthService.uploadProfilePicture.mockReturnValue(throwError(() => new Error('x')));

    component.fileChangeEvent({ target: { files: [new File(['x'], 'pic.png')] } });
    component.fileChangeEvent({ target: { files: [] } });

    expect(mockAuthService.uploadProfilePicture).toHaveBeenCalledTimes(1);
    expect(component.isUploadingPicture()).toBe(false);
    expect(mockMessageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('reports a failed profile picture delete', () => {
    mockAuthService.deleteProfilePicture.mockReturnValue(throwError(() => new Error('x')));
    component.onDeleteProfilePicture();
    expect(mockMessageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });
});
