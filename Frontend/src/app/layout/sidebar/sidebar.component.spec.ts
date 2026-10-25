import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { SidebarComponent } from './sidebar.component';
import { AuthService } from '../../core/auth/auth.service';
import { UserProfileDrawerService } from '../../core/services/user-profile-drawer.service';

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: any;
  let http: { get: ReturnType<typeof vi.fn> };
  let drawerService: { open: ReturnType<typeof vi.fn> };
  let currentUser: ReturnType<typeof signal<any>>;
  let initialized: ReturnType<typeof signal<boolean>>;

  function create() {
    TestBed.configureTestingModule({
      imports: [SidebarComponent],
      providers: [
        {
          provide: AuthService,
          useValue: { currentUser, isInitialized: initialized, isAuthenticated: () => !!currentUser() },
        },
        { provide: HttpClient, useValue: http },
        { provide: UserProfileDrawerService, useValue: drawerService },
      ],
    });
    TestBed.overrideComponent(SidebarComponent, { set: { template: '' } });
    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  const labels = () => component.items().map((group) => group.label);
  const links = () => component.items().flatMap((group) => group.items?.map((i) => i.routerLink) ?? []);

  beforeEach(() => {
    http = { get: vi.fn(() => of({ items: [] })) };
    drawerService = { open: vi.fn() };
    currentUser = signal<any>(null);
    initialized = signal(true);
  });

  it('shows the public menu to anonymous visitors', () => {
    create();
    expect(links()).toEqual(['/', '/leaderboard']);
    expect(component.canAccessQuotes()).toBe(false);
    expect(http.get).not.toHaveBeenCalled();
  });

  it('shows quotes when the quotes API is accessible', () => {
    currentUser.set({ id: 'u1', roles: ['User'] });
    create();
    expect(http.get).toHaveBeenCalledWith('/api/v1/quote', { params: { skip: '0', take: '1' } });
    expect(component.canAccessQuotes()).toBe(true);
    expect(links()).toContain('/quotes');
    expect(links()).not.toContain('/admin/orders');
  });

  it('hides quotes when the user is not allowed to see them', () => {
    http.get.mockReturnValue(throwError(() => ({ status: 403 })));
    currentUser.set({ id: 'u1', roles: ['User'] });
    create();
    expect(component.canAccessQuotes()).toBe(false);
    expect(links()).not.toContain('/quotes');
  });

  it('keeps quotes available on other errors', () => {
    http.get.mockReturnValue(throwError(() => ({ status: 0 })));
    currentUser.set({ id: 'u1', roles: [] });
    create();
    expect(component.canAccessQuotes()).toBe(true);
  });

  it('waits for auth to initialise before probing quotes', () => {
    initialized.set(false);
    currentUser.set({ id: 'u1', roles: [] });
    create();
    expect(http.get).not.toHaveBeenCalled();
  });

  it('shows the orders admin item to kiosks and the full admin menu to admins', () => {
    currentUser.set({ id: 'k', roles: ['Kiosk'] });
    create();
    expect(links()).toContain('/admin/orders');
    expect(links()).not.toContain('/admin/users');

    currentUser.set({ id: 'a', roles: ['Admin'] });
    fixture.detectChanges();
    expect(links()).toContain('/admin/users');
    expect(links()).toContain('/admin/global-settings');
    expect(labels().length).toBe(2);
  });

  it('opens the own profile and closes the sidebar', () => {
    create();
    const closed = vi.fn();
    component.close.subscribe(closed);

    component.openOwnProfile();
    expect(drawerService.open).not.toHaveBeenCalled();

    currentUser.set({ id: 'u1', roles: [] });
    component.openOwnProfile();
    expect(drawerService.open).toHaveBeenCalledWith('u1');
    expect(closed).toHaveBeenCalled();
  });
});
