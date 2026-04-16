import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root'
})
export class PushNotificationService {
  private http = inject(HttpClient);
  private swPush = inject(SwPush);
  private authService = inject(AuthService);
  private apiUrl = `${environment.apiUrl}/api/v1/notifications`;

  private promptDismissed = signal<boolean>(localStorage.getItem('push_prompt_shown') === 'true');

  isEnabled = signal<boolean>(false);
  isSupported = signal<boolean>(this.swPush.isEnabled);

  // Signal for prompt visibility
  showPrompt = computed(() => {
    return !this.promptDismissed() &&
           this.authService.isAuthenticated() &&
           this.isSupported() && 
           Notification.permission === 'default' && 
           !this.isEnabled();
  });

  constructor() {
    // Sync status if supported
    if (this.isSupported()) {
      this.swPush.subscription.subscribe(sub => {
        this.isEnabled.set(!!sub);
      });
    }
  }

  async getVapidPublicKey(): Promise<string> {
    return firstValueFrom(this.http.get(`${this.apiUrl}/vapid-public-key`, { responseType: 'text' }));
  }

  async subscribe() {
    if (!this.isSupported()) return;

    try {
      const vapidPublicKey = await this.getVapidPublicKey();
      
      if (!vapidPublicKey || vapidPublicKey.length < 20 || vapidPublicKey.includes('not configured')) {
        console.error('Invalid VAPID Public Key received from server:', vapidPublicKey);
        throw new Error('VAPID Public Key is not correctly configured on the server.');
      }

      const sub = await this.swPush.requestSubscription({
        serverPublicKey: vapidPublicKey
      });

      const subObj = sub.toJSON();
      await firstValueFrom(this.http.post(`${this.apiUrl}/subscribe`, {
        endpoint: subObj.endpoint,
        p256dh: subObj.keys?.['p256dh'],
        auth: subObj.keys?.['auth']
      }));

      this.isEnabled.set(true);
      this.setPrompted(); // Hide prompt after successful subscription
    } catch (err) {
      console.error('Could not subscribe to notifications', err);
      throw err;
    }
  }

  async unsubscribe() {
    if (!this.isSupported()) return;

    try {
      const sub = await firstValueFrom(this.swPush.subscription);
      if (sub) {
        // Wrap endpoint in an object to match the new backend DTO
        try {
          await firstValueFrom(this.http.post(`${this.apiUrl}/unsubscribe`, { endpoint: sub.endpoint }));
        } catch (apiErr) {
          // We proceed with browser unsubscription even if backend fails to ensure local UI is consistent
        }
        
        await this.swPush.unsubscribe();
      }
      this.isEnabled.set(false);
    } catch (err) {
      console.error('Could not unsubscribe from notifications', err);
      throw err;
    }
  }

  setPrompted() {
    localStorage.setItem('push_prompt_shown', 'true');
    this.promptDismissed.set(true);
  }
}
