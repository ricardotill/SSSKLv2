import { TestBed } from '@angular/core/testing';
import { SwUpdate } from '@angular/service-worker';
import { Subject, of } from 'rxjs';
import { vi } from 'vitest';
import { AppUpdateService, WINDOW } from './app-update.service';
import { AppVersionService } from './app-version.service';

describe('AppUpdateService', () => {
  function createVersionService(version = '3.8.3') {
    return {
      refresh: vi.fn(() => of({ version }))
    };
  }

  it('should do nothing when service workers are disabled', () => {
    const swUpdateMock = {
      isEnabled: false,
      versionUpdates: new Subject(),
      checkForUpdate: vi.fn(),
      activateUpdate: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        AppUpdateService,
        { provide: SwUpdate, useValue: swUpdateMock },
        { provide: AppVersionService, useValue: createVersionService() }
      ]
    });

    TestBed.inject(AppUpdateService);

    expect(swUpdateMock.checkForUpdate).not.toHaveBeenCalled();
    expect(swUpdateMock.activateUpdate).not.toHaveBeenCalled();
  });

  it('should check, activate, and reload when a new version is ready', async () => {
    const versionUpdates = new Subject();
    const reload = vi.fn();
    const swUpdateMock = {
      isEnabled: true,
      versionUpdates: versionUpdates.asObservable(),
      checkForUpdate: vi.fn().mockResolvedValue(true),
      activateUpdate: vi.fn().mockResolvedValue(true)
    };
    const appVersionServiceMock = createVersionService();

    TestBed.configureTestingModule({
      providers: [
        AppUpdateService,
        { provide: SwUpdate, useValue: swUpdateMock },
        { provide: AppVersionService, useValue: appVersionServiceMock },
        { provide: WINDOW, useValue: { location: { reload }, setInterval: vi.fn() } }
      ]
    });

    TestBed.inject(AppUpdateService);
    await vi.waitFor(() => expect(swUpdateMock.checkForUpdate).toHaveBeenCalled());

    versionUpdates.next({
      type: 'VERSION_READY',
      currentVersion: { hash: 'old' },
      latestVersion: { hash: 'new' }
    });

    await vi.waitFor(() => {
      expect(swUpdateMock.activateUpdate).toHaveBeenCalled();
      expect(reload).toHaveBeenCalled();
    });
  });

  it('should still reload when activation fails', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {});
    const versionUpdates = new Subject();
    const reload = vi.fn();
    const swUpdateMock = {
      isEnabled: true,
      versionUpdates: versionUpdates.asObservable(),
      checkForUpdate: vi.fn().mockResolvedValue(true),
      activateUpdate: vi.fn().mockRejectedValue(new Error('Activation failed'))
    };
    const appVersionServiceMock = createVersionService();

    TestBed.configureTestingModule({
      providers: [
        AppUpdateService,
        { provide: SwUpdate, useValue: swUpdateMock },
        { provide: AppVersionService, useValue: appVersionServiceMock },
        { provide: WINDOW, useValue: { location: { reload }, setInterval: vi.fn() } }
      ]
    });

    TestBed.inject(AppUpdateService);

    versionUpdates.next({
      type: 'VERSION_READY',
      currentVersion: { hash: 'old' },
      latestVersion: { hash: 'new' }
    });

    await vi.waitFor(() => {
      expect(swUpdateMock.activateUpdate).toHaveBeenCalled();
      expect(reload).toHaveBeenCalled();
    });
  });

  it('should re-check for service worker updates when the deployed version differs', async () => {
    const versionUpdates = new Subject();
    const swUpdateMock = {
      isEnabled: true,
      versionUpdates: versionUpdates.asObservable(),
      checkForUpdate: vi.fn().mockResolvedValue(true),
      activateUpdate: vi.fn().mockResolvedValue(true)
    };
    const appVersionServiceMock = createVersionService('3.8.4');

    TestBed.configureTestingModule({
      providers: [
        AppUpdateService,
        { provide: SwUpdate, useValue: swUpdateMock },
        { provide: AppVersionService, useValue: appVersionServiceMock },
        { provide: WINDOW, useValue: { location: { reload: vi.fn() }, setInterval: vi.fn((callback: () => void) => callback()) } }
      ]
    });

    TestBed.inject(AppUpdateService);

    await vi.waitFor(() => {
      expect(appVersionServiceMock.refresh).toHaveBeenCalled();
      expect(swUpdateMock.checkForUpdate).toHaveBeenCalled();
    });
  });
});
