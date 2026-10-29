import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { NotificationService } from './notification.service';
import { environment } from '../../../environments/environment';

describe('NotificationService', () => {
  let service: NotificationService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/api/v1/notifications`;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(NotificationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('fetches a page of notifications', () => {
    service.getNotifications(true, 6, 3).subscribe();
    const req = httpMock.expectOne((r) => r.url === apiUrl);
    expect(req.request.params.get('unreadOnly')).toBe('true');
    expect(req.request.params.get('skip')).toBe('6');
    expect(req.request.params.get('take')).toBe('3');
    req.flush([]);
  });

  it('tracks the unread count', () => {
    service.fetchUnreadCount().subscribe();
    httpMock.expectOne(`${apiUrl}/unread-count`).flush(2);
    expect(service.unreadCount()).toBe(2);

    service.markAsRead('n1').subscribe();
    httpMock.expectOne(`${apiUrl}/n1/read`).flush(null);
    expect(service.unreadCount()).toBe(1);

    service.markAllAsRead().subscribe();
    httpMock.expectOne(`${apiUrl}/read-all`).flush(null);
    expect(service.unreadCount()).toBe(0);

    service.markAsRead('n2').subscribe();
    httpMock.expectOne(`${apiUrl}/n2/read`).flush(null);
    expect(service.unreadCount()).toBe(0);
  });

  it('sends a custom notification', () => {
    const dto = { title: 'Hoi', message: 'Bericht', fanOut: true };
    service.sendCustomNotification(dto).subscribe();
    const req = httpMock.expectOne(`${apiUrl}/custom`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(dto);
    req.flush(null);
  });
});
