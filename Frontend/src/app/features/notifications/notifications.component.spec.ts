import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { NotificationsComponent } from './notifications.component';
import { NotificationService } from '../../core/services/notification.service';

describe('NotificationsComponent', () => {
  let component: NotificationsComponent;
  let notificationService: any;
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  const notifications = [
    { id: 'n1', isRead: false, linkUri: '/events/1' },
    { id: 'n2', isRead: true, linkUri: null },
  ] as any[];

  beforeEach(() => {
    notificationService = {
      getNotifications: vi.fn(() => of(notifications)),
      markAsRead: vi.fn(() => of(void 0)),
      markAllAsRead: vi.fn(() => of(void 0)),
    };
    router = { navigateByUrl: vi.fn() };
    TestBed.configureTestingModule({
      imports: [NotificationsComponent],
      providers: [
        { provide: NotificationService, useValue: notificationService },
        { provide: Router, useValue: router },
      ],
    });
    TestBed.overrideComponent(NotificationsComponent, { set: { template: '' } });
    const fixture = TestBed.createComponent(NotificationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the first page of notifications', () => {
    expect(notificationService.getNotifications).toHaveBeenCalledWith(false, 0, 20);
    expect(component.notifications()).toEqual(notifications);
    expect(component.loading()).toBe(false);
  });

  it('pages through notifications', () => {
    component.prevPage();
    expect(notificationService.getNotifications).toHaveBeenCalledTimes(1);

    component.nextPage();
    expect(notificationService.getNotifications).toHaveBeenLastCalledWith(false, 20, 20);

    component.prevPage();
    expect(notificationService.getNotifications).toHaveBeenLastCalledWith(false, 0, 20);

    component.onPageChange({ first: 40 });
    expect(notificationService.getNotifications).toHaveBeenLastCalledWith(false, 40, 20);
  });

  it('stops loading when notifications fail to load', () => {
    notificationService.getNotifications.mockReturnValue(throwError(() => new Error('x')));
    component.loadNotifications();
    expect(component.loading()).toBe(false);
  });

  it('marks an unread notification as read and follows its link', () => {
    component.onNotificationClick(notifications[0]);
    expect(notificationService.markAsRead).toHaveBeenCalledWith('n1');
    expect(component.notifications()[0].isRead).toBe(true);
    expect(router.navigateByUrl).toHaveBeenCalledWith('/events/1');

    component.onNotificationClick(notifications[1]);
    expect(notificationService.markAsRead).toHaveBeenCalledTimes(1);
    expect(router.navigateByUrl).toHaveBeenCalledTimes(1);
  });

  it('marks all notifications as read', () => {
    component.markAllAsRead();
    expect(component.notifications().every((n) => n.isRead)).toBe(true);
    expect(component.markingAll()).toBe(false);

    notificationService.markAllAsRead.mockReturnValue(throwError(() => new Error('x')));
    component.markAllAsRead();
    expect(component.markingAll()).toBe(false);
  });
});
