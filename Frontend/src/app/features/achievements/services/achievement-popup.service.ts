import { inject, Injectable, signal } from '@angular/core';
import { AchievementEntry } from '../../../core/models/achievement.model';
import { AchievementService } from './achievement.service';

@Injectable({
  providedIn: 'root'
})
export class AchievementPopupService {
  private achievementService = inject(AchievementService);

  readonly unseenEntries = signal<AchievementEntry[] | null>(null);

  checkUnseenAchievements() {
    this.achievementService.getUnseenAchievementEntries().subscribe({
      next: (entries) => {
        if (entries && entries.length > 0) {
          this.unseenEntries.set(entries);
        }
      },
      error: (err) => console.error('Failed to fetch unseen achievements', err)
    });
  }

  clear() {
    this.unseenEntries.set(null);
  }
}
