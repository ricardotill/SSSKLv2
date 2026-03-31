import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { RoleService } from './role.service';
import { CreateRoleRequest } from '../../../core/models/role.model';

describe('RoleService', () => {
  let service: RoleService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        RoleService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(RoleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch roles', () => {
    service.getAllRoles().subscribe();
    const req = httpMock.expectOne('/api/v1/roles');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should create role', () => {
    const role: CreateRoleRequest = { name: 'Admin' };
    service.createRole(role).subscribe();
    const req = httpMock.expectOne('/api/v1/roles');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(role);
    req.flush({});
  });
});
