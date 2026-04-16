import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UserProfileDrawerService {
  readonly drawerVisible = signal(false);
  readonly selectedUserId = signal<string | null>(null);

  open(userId: string): void {
    this.selectedUserId.set(userId);
    this.drawerVisible.set(true);
  }

  close(): void {
    this.drawerVisible.set(false);
    this.selectedUserId.set(null);
  }
}
