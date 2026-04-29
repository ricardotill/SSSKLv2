import { Injectable, InjectionToken, inject } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter, firstValueFrom } from 'rxjs';
import { AppVersionService, CURRENT_APP_VERSION } from './app-version.service';

export const WINDOW = new InjectionToken<Window | null>('Window', {
  providedIn: 'root',
  factory: () => typeof window === 'undefined' ? null : window
});

@Injectable({
  providedIn: 'root'
})
export class AppUpdateService {
  private readonly swUpdate = inject(SwUpdate);
  private readonly windowRef = inject(WINDOW);
  private readonly appVersionService = inject(AppVersionService);
  private updateCheckInProgress = false;

  constructor() {
    if (!this.swUpdate.isEnabled) {
      return;
    }

    this.swUpdate.versionUpdates
      .pipe(filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'))
      .subscribe(() => {
        void this.reloadForUpdate();
      });

    void this.checkForAvailableUpdate(true);
    this.windowRef?.setInterval(() => void this.checkForAvailableUpdate(), 60_000);
  }

  private async checkForAvailableUpdate(force = false): Promise<void> {
    if (this.updateCheckInProgress) {
      return;
    }

    this.updateCheckInProgress = true;
    try {
      const remoteVersion = await firstValueFrom(this.appVersionService.refresh());
      if (force || remoteVersion.version !== CURRENT_APP_VERSION) {
        await this.swUpdate.checkForUpdate();
      }
    } catch (error) {
      console.error('Failed to check for an app update.', error);
    } finally {
      this.updateCheckInProgress = false;
    }
  }

  private async reloadForUpdate(): Promise<void> {
    try {
      await this.swUpdate.activateUpdate();
    } catch {
      // Reload anyway; the browser will load through the newest active service worker it has.
    } finally {
      this.windowRef?.location.reload();
    }
  }
}
