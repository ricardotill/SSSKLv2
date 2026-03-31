import { inject, Injectable, signal, effect } from '@angular/core';
import { GlobalSettingsService } from './global-settings.service';
import { AuthService } from '../../../core/auth/auth.service';
import { take } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class WhatsNewService {
  private readonly settingsService = inject(GlobalSettingsService);
  private readonly authService = inject(AuthService);

  private readonly STORAGE_KEY = 'ssskl_whats_new_seen_version';
  
  readonly content = signal<string | null>(null);
  readonly isVisible = signal<boolean>(false);
  private currentVersion = '';

  constructor() {
    // Check whenever the user becomes authenticated and initialized
    effect(() => {
      const user = this.authService.currentUser();
      const isInit = this.authService.isInitialized();

      if (isInit && user) {
        this.checkWhatsNew();
      }
    });
  }

  private checkWhatsNew() {
    this.settingsService.getSetting('WhatsNewContent').subscribe({
      next: (setting) => {
        const lastSeenVersion = localStorage.getItem(this.STORAGE_KEY);
        const currentVersion = setting.updatedOn; // Using updatedOn as version

        if (lastSeenVersion !== currentVersion) {
          this.content.set(setting.value);
          this.currentVersion = currentVersion;
          this.isVisible.set(true);
        }
      },
      error: (err) => {
        if (err.status !== 404) {
          console.error('Failed to fetch Whats New content', err);
        }
      }
    });
  }

  markAsSeen() {
    if (this.currentVersion) {
      localStorage.setItem(this.STORAGE_KEY, this.currentVersion);
    }
    this.isVisible.set(false);
  }
}
