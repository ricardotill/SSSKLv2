import { Component, ChangeDetectionStrategy, inject, effect, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { CarouselModule } from 'primeng/carousel';
import { AchievementPopupService } from '../../../core/services/achievement-popup.service';
import confetti from 'canvas-confetti';
import { LanguageService } from '../../../core/services/language.service';
import { AchievementEntry } from '../../../core/models/achievement.model';

@Component({
  selector: 'app-achievement-modal',
  standalone: true,
  imports: [CommonModule, DialogModule, ButtonModule, CarouselModule],
  template: `
    <p-dialog 
      [(visible)]="isVisible" 
      [modal]="true" 
      [closable]="false"
      (onHide)="onHide()"
      [style]="{ width: '90vw', maxWidth: '500px' }"
      [draggable]="false"
      [resizable]="false"
      [showHeader]="false"
      styleClass="rounded-2xl overflow-hidden border-none"
      contentStyleClass="p-0">
      
      @if (isVisible) {
        <div class="relative w-full animate-fade-in-up flex flex-col">
          
          <!-- Header / Title -->
          <div class="pt-8 pb-4 px-6 text-center">
            <h2 class="text-3xl font-bold font-heading mb-2 text-primary dark:text-primary-400">
              {{ ls.translate('new_achievement', { s: entries.length > 1 ? 's' : '' }) }}
            </h2>
            <p class="text-surface-500 dark:text-surface-400 font-body">
              {{ ls.t().new_achievement_desc }}
            </p>
          </div>

          <!-- Content Carousel if multiple, otherwise single -->
          <div class="py-6 px-6 bg-surface-50 dark:bg-surface-800 rounded-2xl mx-6 mb-2">
            @if (entries.length > 1) {
              <p-carousel styleClass="rounded-full" [value]="entries" [numVisible]="1" [numScroll]="1" [circular]="false" [autoplayInterval]="0">
                <ng-template pTemplate="item" let-entry>
                  <div class="flex flex-col items-center justify-center p-4">
                    <div class="w-32 h-32 mb-6 rounded-full overflow-hidden shadow-lg border-4 border-primary/20 flex items-center justify-center bg-surface-100 dark:bg-surface-700">
                      @if (entry.imageUrl) {
                        <img [src]="entry.imageUrl" [alt]="entry.achievementName" class="w-full h-full object-cover" />
                      } @else {
                        <i class="pi pi-star-fill text-5xl text-yellow-500"></i>
                      }
                    </div>
                    <h3 class="text-2xl font-bold text-surface-900 dark:text-surface-0 text-center mb-3">
                      {{ entry.achievementName }}
                    </h3>
                  </div>
                </ng-template>
              </p-carousel>
            } @else {
              <div class="flex flex-col items-center justify-center p-4">
                <div class="w-32 h-32 mb-6 rounded-full overflow-hidden shadow-lg border-4 border-primary/20 flex items-center justify-center bg-surface-100 dark:bg-surface-700">
                  @if (entries[0].imageUrl) {
                    <img [src]="entries[0].imageUrl" [alt]="entries[0].achievementName" class="w-full h-full object-cover" />
                  } @else {
                    <i class="pi pi-star-fill text-5xl text-yellow-500"></i>
                  }
                </div>
                <h3 class="text-2xl font-bold text-surface-900 dark:text-surface-0 text-center mb-3">
                  {{ entries[0].achievementName }}
                </h3>
              </div>
            }
          </div>

          <!-- Footer / Action -->
          <div class="p-6 text-center">
            <p-button 
              [label]="ls.t().awesome" 
              icon="pi pi-check" 
              (onClick)="close()" 
              styleClass="p-button-primary p-button-rounded w-full max-w-xs font-bold text-lg">
            </p-button>
          </div>
        </div>
      }
    </p-dialog>
  `,
  styles: [`
    @keyframes fadeInUp {
      from { opacity: 0; transform: translateY(20px) scale(0.95); }
      to { opacity: 1; transform: translateY(0) scale(1); }
    }
    .animate-fade-in-up {
      animation: fadeInUp 0.4s cubic-bezier(0.16, 1, 0.3, 1) forwards;
    }
    ::ng-deep .p-dialog-mask {
      backdrop-filter: blur(4px);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AchievementModalComponent {
  private popupService = inject(AchievementPopupService);
  private cdr = inject(ChangeDetectorRef);
  ls = inject(LanguageService);

  isVisible = false;
  entries: AchievementEntry[] = [];

  constructor() {
    effect(() => {
      const unseen = this.popupService.unseenEntries();
      if (unseen && unseen.length > 0) {
        this.entries = unseen;
        this.isVisible = true;
        this.fireConfetti();
      } else {
        this.isVisible = false;
        this.entries = [];
      }
      this.cdr.markForCheck();
    });
  }

  close() {
    this.isVisible = false;
    setTimeout(() => {
      this.popupService.clear();
    }, 300);
  }

  onHide() {
    this.popupService.clear();
  }

  private fireConfetti() {
    const duration = 3000;
    const end = Date.now() + duration;

    const frame = () => {
      confetti({
        particleCount: 5,
        angle: 60,
        spread: 55,
        origin: { x: 0 },
        colors: ['#26ccff', '#a25afd', '#ff5e7e', '#88ff5a', '#fcff42', '#ffa62d', '#ff36ff'],
        zIndex: 9999
      });
      confetti({
        particleCount: 5,
        angle: 120,
        spread: 55,
        origin: { x: 1 },
        colors: ['#26ccff', '#a25afd', '#ff5e7e', '#88ff5a', '#fcff42', '#ffa62d', '#ff36ff'],
        zIndex: 9999
      });

      if (Date.now() < end) {
        requestAnimationFrame(frame);
      }
    };

    frame();
  }
}
