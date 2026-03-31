import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { GlobalSettingsService } from './global-settings.service';
import { GlobalSetting } from '../../../core/models/global-settings.model';

describe('GlobalSettingsService', () => {
  let service: GlobalSettingsService;
  let httpMock: HttpTestingController;

  const mockSetting: GlobalSetting = { key: 'TestKey', value: 'Value', updatedOn: '2023-01-01' };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        GlobalSettingsService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(GlobalSettingsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    TestBed.resetTestingModule();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch setting', () => {
    service.getSetting('TestKey').subscribe((res) => {
      expect(res.key).toBe('TestKey');
    });
    const req = httpMock.expectOne('/api/v1/GlobalSettings/TestKey');
    expect(req.request.method).toBe('GET');
    req.flush(mockSetting);
  });

  it('should update setting', () => {
    const dto = { value: 'NewValue' };
    service.updateSetting('TestKey', dto as any).subscribe();
    const req = httpMock.expectOne('/api/v1/GlobalSettings/TestKey');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(dto);
    req.flush(null);
  });
});
