import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { NotificationDrawerComponent } from './notification-drawer.component';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationDrawerService } from '../../../core/services/notification-drawer.service';

describe('NotificationDrawerComponent', () => {
  let component: NotificationDrawerComponent;
  let fixture: any;
  let notificationService: any;
  let drawerService: { drawerVisible: ReturnType<typeof signal<boolean>>; close: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn>; navigateByUrl: ReturnType<typeof vi.fn> };

  const notifications = [
    { id: 'n1', title: 'A', linkUri: '/quotes/1' },
    { id: 'n2', title: 'B', linkUri: null },
  ] as any[];

  beforeEach(() => {
    notificationService = {
      getNotifications: vi.fn(() => of(notifications)),
      markAsRead: vi.fn(() => of(void 0)),
      markAllAsRead: vi.fn(() => of(void 0)),
    };
    drawerService = { drawerVisible: signal(false), close: vi.fn() };
    router = { navigate: vi.fn(), navigateByUrl: vi.fn() };

    TestBed.configureTestingModule({
      imports: [NotificationDrawerComponent],
      providers: [
        { provide: NotificationService, useValue: notificationService },
        { provide: NotificationDrawerService, useValue: drawerService },
        { provide: Router, useValue: router },
      ],
    });
    TestBed.overrideComponent(NotificationDrawerComponent, { set: { template: '' } });
    fixture = TestBed.createComponent(NotificationDrawerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads unread notifications when opened', () => {
    expect(notificationService.getNotifications).not.toHaveBeenCalled();
    drawerService.drawerVisible.set(true);
    fixture.detectChanges();

    expect(notificationService.getNotifications).toHaveBeenCalledWith(true, 0, 6);
    expect(component.notifications()).toEqual(notifications);
    expect(component.loading()).toBe(false);
  });

  it('marks one or all notifications as read', () => {
    component.notifications.set(notifications);
    component.markAsRead(notifications[0]);
    expect(component.notifications().map((n) => n.id)).toEqual(['n2']);

    component.markAllAsRead();
    expect(component.notifications()).toEqual([]);
    expect(component.markingAll()).toBe(false);
  });

  it('follows a notification link', () => {
    component.onNotificationClick(notifications[0]);
    expect(notificationService.markAsRead).toHaveBeenCalledWith('n1');
    expect(drawerService.close).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/quotes/1');

    router.navigateByUrl.mockClear();
    component.onNotificationClick(notifications[1]);
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('opens the notifications page', () => {
    component.viewAll();
    expect(drawerService.close).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/notifications']);
  });

  it('closes when hidden or on an outside click', () => {
    component.onVisibleChange(true);
    expect(drawerService.close).not.toHaveBeenCalled();
    component.onVisibleChange(false);
    expect(drawerService.close).toHaveBeenCalledTimes(1);

    component.onDocumentClick({ target: document.body } as any);
    expect(drawerService.close).toHaveBeenCalledTimes(1);

    drawerService.drawerVisible.set(true);
    const header = document.createElement('app-header');
    const bell = document.createElement('button');
    header.appendChild(bell);
    document.body.appendChild(header);
    component.onDocumentClick({ target: bell } as any);
    expect(drawerService.close).toHaveBeenCalledTimes(1);
    header.remove();

    component.onDocumentClick({ target: document.body } as any);
    expect(drawerService.close).toHaveBeenCalledTimes(2);
  });
});
