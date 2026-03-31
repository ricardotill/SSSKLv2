import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ApplicationUserService } from './application-user.service';
import { PaginatedUsers } from '../../../core/models/application-user.model';

describe('ApplicationUserService', () => {
  let service: ApplicationUserService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ApplicationUserService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(ApplicationUserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch users', () => {
    const mockResponse: PaginatedUsers = { items: [], totalCount: 0 };
    service.getUsers().subscribe();
    const req = httpMock.expectOne((req) => req.url === '/api/v1/applicationuser');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should fetch user by id', () => {
    service.getUser('1').subscribe();
    const req = httpMock.expectOne('/api/v1/applicationuser/1');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('should delete user profile picture', () => {
    service.deleteProfilePicture('1').subscribe();
    const req = httpMock.expectOne('/api/v1/applicationuser/1/profile-picture');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
