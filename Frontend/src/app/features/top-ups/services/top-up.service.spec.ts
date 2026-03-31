import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TopUpService } from './top-up.service';

describe('TopUpService', () => {
  let service: TopUpService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TopUpService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(TopUpService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch top-ups', () => {
    service.getTopUps(0, 10).subscribe();
    const req = httpMock.expectOne((req) => req.url === '/api/v1/TopUp');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('skip')).toBe('0');
    req.flush({ items: [], totalCount: 0 });
  });

  it('should create top-up', () => {
    const topUp = { userId: '1', amount: 50 };
    service.createTopUp(topUp as any).subscribe();
    const req = httpMock.expectOne('/api/v1/TopUp');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(topUp);
    req.flush({});
  });
});
