import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { NotificationDrawerComponent } from './notification-drawer.component';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationDrawerService } from '../../../core/services/notification-drawer.service';

describe('NotificationDrawerComponent (rendered)', () => {
  let fixture: ComponentFixture<NotificationDrawerComponent>;
  let component: NotificationDrawerComponent;
  let notificationService: any;
  let drawerService: { drawerVisible: ReturnType<typeof signal<boolean>>; close: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn>; navigateByUrl: ReturnType<typeof vi.fn> };

  const notifications = [
    { id: 'n1', title: 'Nieuwe quote', message: 'Bekijk hem', createdOn: '2026-01-01T10:00:00Z', linkUri: '/quotes/1' },
    { id: 'n2', title: 'Nieuw event', message: 'Kom ook', createdOn: '2026-01-01T11:00:00Z', linkUri: null },
  ] as any[];

  const drawer = () => document.querySelector('.notification-drawer') as HTMLElement;

  function open(items: any[], unread: number) {
    notificationService.getNotifications.mockReturnValue(of(items));
    notificationService.unreadCount.set(unread);
    drawerService.drawerVisible.set(true);
    fixture.detectChanges();
  }

  beforeEach(() => {
    notificationService = {
      unreadCount: signal(0),
      getNotifications: vi.fn(() => of(notifications)),
      markAsRead: vi.fn(() => of(void 0)),
      markAllAsRead: vi.fn(() => of(void 0)),
    };
    drawerService = { drawerVisible: signal(false), close: vi.fn() };
    router = { navigate: vi.fn(), navigateByUrl: vi.fn() };

    TestBed.configureTestingModule({
      imports: [NotificationDrawerComponent, NoopAnimationsModule],
      providers: [
        { provide: NotificationService, useValue: notificationService },
        { provide: NotificationDrawerService, useValue: drawerService },
        { provide: Router, useValue: router },
      ],
    });
    fixture = TestBed.createComponent(NotificationDrawerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => fixture.destroy());

  it('renders the notifications and a link to all unread ones', () => {
    open(notifications, 5);

    expect(drawer().textContent).toContain('Nieuwe quote');
    expect(drawer().textContent).toContain('Kom ook');
    expect(drawer().textContent).toContain('Alles gelezen');
    expect(drawer().textContent).toContain('Bekijk alle 5 notificaties');
  });

  it('opens a notification and marks one as read from its button', () => {
    open(notifications, 2);

    drawer().querySelector<HTMLElement>('.group')!.click();
    expect(notificationService.markAsRead).toHaveBeenCalledWith('n1');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/quotes/1');

    drawer().querySelector<HTMLButtonElement>('p-button[title="Gelezen markeren"] button')!.click();
    expect(notificationService.markAsRead).toHaveBeenCalledTimes(2);
  });

  it('shows the empty state and a link to old notifications', () => {
    open([], 0);

    expect(drawer().textContent).toContain('Geen nieuwe notificaties');
    expect(drawer().textContent).toContain('Bekijk oude notificaties');
    expect(drawer().textContent).not.toContain('Alles gelezen');
  });

  it('shows a spinner while loading', () => {
    open(notifications, 0);
    component.loading.set(true);
    fixture.detectChanges();

    expect(drawer().querySelector('p-progress-spinner')).toBeTruthy();
  });
});
