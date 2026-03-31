import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PublicService } from './public.service';

describe('PublicService', () => {
  let service: PublicService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        PublicService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(PublicService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch domain', () => {
    service.getDomain().subscribe((res) => {
      expect(res).toBe('test-domain');
    });

    const req = httpMock.expectOne('/api/v1/public/domain');
    expect(req.request.method).toBe('GET');
    req.flush('test-domain');
  });
});
