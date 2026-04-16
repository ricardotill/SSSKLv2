import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class NotificationDrawerService {
  readonly drawerVisible = signal(false);

  open(): void {
    this.drawerVisible.set(true);
  }

  close(): void {
    this.drawerVisible.set(false);
  }
}
