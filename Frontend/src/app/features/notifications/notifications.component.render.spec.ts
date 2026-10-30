import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { NotificationsComponent } from './notifications.component';
import { NotificationService } from '../../core/services/notification.service';

describe('NotificationsComponent (rendered)', () => {
  let fixture: ComponentFixture<NotificationsComponent>;
  let component: NotificationsComponent;
  let notificationService: any;
  let router: { navigateByUrl: ReturnType<typeof vi.fn> };

  const notifications = [
    { id: 'n1', title: 'Ongelezen', message: 'Nieuw', isRead: false, createdOn: '2026-01-01T10:00:00Z', linkUri: '/quotes/1' },
    { id: 'n2', title: 'Gelezen', message: 'Oud', isRead: true, createdOn: '2026-01-01T09:00:00Z', linkUri: null },
  ] as any[];

  const el = () => fixture.nativeElement as HTMLElement;

  function create(items: any[], unread: number) {
    notificationService = {
      unreadCount: signal(unread),
      getNotifications: vi.fn(() => of(items)),
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
    fixture = TestBed.createComponent(NotificationsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('renders read and unread notifications and opens one on click', () => {
    create(notifications, 1);

    expect(el().textContent).toContain('Alles gelezen');
    expect(el().textContent).toContain('Ongelezen');
    expect(el().querySelector('.pi-envelope')).toBeTruthy();
    expect(el().querySelector('.pi-bell')).toBeTruthy();

    el().querySelector<HTMLElement>('.cursor-pointer')!.click();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/quotes/1');
  });

  it('renders a paginator when there are more notifications than fit on a page', () => {
    create(notifications, 0);
    component.totalRecords.set(50);
    fixture.detectChanges();

    expect(el().querySelector('p-paginator')).toBeTruthy();
    expect(el().textContent).not.toContain('Alles gelezen');
  });

  it('renders the empty state', () => {
    create([], 0);
    expect(el().textContent).toContain('Nog geen notificaties');
  });

  it('shows a spinner while loading the first page', () => {
    create([], 0);
    component.loading.set(true);
    fixture.detectChanges();
    expect(el().querySelector('p-progress-spinner')).toBeTruthy();
  });
});
