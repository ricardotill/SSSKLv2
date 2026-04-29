import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AppVersionService } from './app-version.service';

describe('AppVersionService', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AppVersionService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch the public app version on creation', () => {
    const service = TestBed.inject(AppVersionService);

    const req = httpMock.expectOne('/api/v1/public/version');
    expect(req.request.method).toBe('GET');
    req.flush({ version: '3.8.5' });

    expect(service.version()).toBe('3.8.5');
  });

  it('should fall back to the current known version when the endpoint fails', () => {
    const service = TestBed.inject(AppVersionService);

    const req = httpMock.expectOne('/api/v1/public/version');
    req.flush('Nope', { status: 500, statusText: 'Server Error' });

    expect(service.version()).toBe('3.8.5');
  });
});
