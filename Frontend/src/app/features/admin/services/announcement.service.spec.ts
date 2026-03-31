import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AnnouncementService } from './announcement.service';
import { PaginatedAnnouncements } from '../../../core/models/announcement.model';

describe('AnnouncementService', () => {
  let service: AnnouncementService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AnnouncementService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(AnnouncementService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch announcements', () => {
    const mockResponse: PaginatedAnnouncements = {
      items: [{ id: '1', message: 'Test Announcement', order: 1, isScheduled: false, createdOn: '' }],
      totalCount: 1
    };

    service.getAnnouncements().subscribe((res) => {
      expect(res.items.length).toBe(1);
      expect(res.items[0].message).toBe('Test Announcement');
    });

    const req = httpMock.expectOne((req) => req.url === '/api/v1/announcement');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should delete announcement', () => {
    service.deleteAnnouncement('1').subscribe();
    const req = httpMock.expectOne('/api/v1/announcement/1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
