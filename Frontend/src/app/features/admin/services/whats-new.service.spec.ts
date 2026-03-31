import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { WhatsNewService } from './whats-new.service';
import { GlobalSettingsService } from './global-settings.service';
import { AuthService } from '../../../core/auth/auth.service';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { vi } from 'vitest';

describe('WhatsNewService', () => {
  let service: WhatsNewService;
  let httpMock: HttpTestingController;
  let mockSettingsService: any;
  let mockAuthService: any;

  beforeEach(() => {
    mockSettingsService = {
      getSetting: vi.fn().mockReturnValue(of({ value: 'Test', updatedOn: '1.0' }))
    };
    mockAuthService = {
      currentUser: signal({ id: '1' } as any),
      isInitialized: signal(true)
    };

    TestBed.configureTestingModule({
      providers: [
        WhatsNewService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: GlobalSettingsService, useValue: mockSettingsService },
        { provide: AuthService, useValue: mockAuthService }
      ],
    });
    service = TestBed.inject(WhatsNewService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should handle isVisible and content signals', () => {
    expect(service.isVisible()).toBe(false);
    expect(service.content()).toBeNull();
  });

  it('should mark as seen', () => {
    // Manually setting internal version for test
    (service as any).currentVersion = '1.0';
    service.markAsSeen();
    expect(localStorage.getItem('ssskl_whats_new_seen_version')).toBe('1.0');
    expect(service.isVisible()).toBe(false);
  });
});
