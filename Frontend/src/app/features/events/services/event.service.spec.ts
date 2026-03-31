import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { EventService } from './event.service';
import { EventDto, PaginationObject } from '../../../core/models/event.model';

describe('EventService', () => {
  let service: EventService;
  let httpMock: HttpTestingController;

  const mockResponse: PaginationObject<EventDto> = {
    items: [{ id: '1', title: 'Test Event', description: 'Test' } as any],
    totalCount: 1
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        EventService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(EventService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch events', () => {
    service.getEvents().subscribe((res) => {
      expect(res.items.length).toBe(1);
      expect(res.items[0].title).toBe('Test Event');
    });

    const req = httpMock.expectOne((req) => req.url === '/api/v1/Events');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
