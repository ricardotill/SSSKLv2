import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { WhatsNewService } from './whats-new.service';
import { GlobalSettingsService } from './global-settings.service';
import { AuthService } from '../../../core/auth/auth.service';
import { of, throwError } from 'rxjs';
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

  it('shows unseen content once the user is signed in', () => {
    TestBed.tick();
    expect(mockSettingsService.getSetting).toHaveBeenCalledWith('WhatsNewContent');
    expect(service.content()).toBe('Test');
    expect(service.isVisible()).toBe(true);
  });

  it('stays hidden when the content was already seen', () => {
    localStorage.setItem('ssskl_whats_new_seen_version', '1.0');
    TestBed.tick();
    expect(service.isVisible()).toBe(false);
  });

  it('stays hidden when no content has been set', () => {
    mockSettingsService.getSetting.mockReturnValue(of(null));
    TestBed.tick();
    expect(service.isVisible()).toBe(false);
    expect(service.content()).toBeNull();
  });

  it('logs when the content cannot be fetched', () => {
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    mockSettingsService.getSetting.mockReturnValue(throwError(() => new Error('boom')));
    TestBed.tick();
    expect(consoleSpy).toHaveBeenCalledWith('Failed to fetch Whats New content', expect.any(Error));
    expect(service.isVisible()).toBe(false);
    consoleSpy.mockRestore();
  });
});
