import { Injectable, inject, signal, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PushNotificationService {
  private http = inject(HttpClient);
  private swPush = inject(SwPush);
  private apiUrl = `${environment.apiUrl}/api/v1/notifications`;

  isEnabled = signal<boolean>(Notification.permission === 'granted');
  isSupported = signal<boolean>(this.swPush.isEnabled);

  // Signal for prompt visibility
  showPrompt = signal<boolean>(false);

  constructor() {
    // Sync status if supported
    if (this.isSupported()) {
      this.swPush.subscription.subscribe(sub => {
        this.isEnabled.set(!!sub);
        this.updatePromptVisibility();
      });
    }

    this.updatePromptVisibility();
  }

  private updatePromptVisibility() {
    const shownInStorage = localStorage.getItem('push_prompt_shown') === 'true';
    const shouldShow = this.isSupported() && 
                       Notification.permission === 'default' && 
                       !shownInStorage && 
                       !this.isEnabled();
    
    // Only update if it actually changes to avoid redundant cycles
    if (this.showPrompt() !== shouldShow) {
      this.showPrompt.set(shouldShow);
    }
  }

  async getVapidPublicKey(): Promise<string> {
    return firstValueFrom(this.http.get(`${this.apiUrl}/vapid-public-key`, { responseType: 'text' }));
  }

  async subscribe() {
    if (!this.isSupported()) return;

    try {
      const vapidPublicKey = await this.getVapidPublicKey();
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
        await firstValueFrom(this.http.post(`${this.apiUrl}/unsubscribe`, sub.endpoint));
        await this.swPush.unsubscribe();
      }
      this.isEnabled.set(false);
      this.updatePromptVisibility();
    } catch (err) {
      console.error('Could not unsubscribe from notifications', err);
      throw err;
    }
  }

  setPrompted() {
    localStorage.setItem('push_prompt_shown', 'true');
    this.showPrompt.set(false);
  }
}
