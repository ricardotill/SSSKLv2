import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { AvatarModule } from 'primeng/avatar';
import { DividerModule } from 'primeng/divider';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ImageModule } from 'primeng/image';
import { AchievementService } from '../services/achievement.service';
import { AuthService } from '../../../core/auth/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { AchievementEntry, AchievementListing } from '../../../core/models/achievement.model';
import { ResolveApiUrlPipe } from '../../../shared/pipes/resolve-api-url.pipe';

@Component({
  selector: 'app-achievement-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    CardModule,
    TagModule,
    AvatarModule,
    DividerModule,
    ProgressSpinnerModule,
    ImageModule,
    DatePipe,
    ResolveApiUrlPipe
  ],
  template: `
    <div class="max-w-4xl mx-auto flex flex-col gap-6">

      @if (loading()) {
        <div class="flex justify-center items-center p-12">
          <p-progressSpinner ariaLabel="loading"></p-progressSpinner>
        </div>
      } @else if (earners().length > 0 || achievementName()) {

        <!-- Header -->
        <div class="flex items-center gap-3">
          <p-button icon="pi pi-arrow-left" [text]="true" severity="secondary" routerLink="/achievements" />
          <div>
            <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ achievementName() }}</h1>
            <p class="text-surface-500 m-0 text-sm mt-1">{{ ls.t()['achievement_detail'] }}</p>
          </div>
        </div>

        <!-- Achievement Info Card -->
        <p-card styleClass="overflow-hidden">
          <div class="flex flex-col md:flex-row gap-6 items-center md:items-start">

            <!-- Badge / Image -->
            <div class="achievement-badge flex-shrink-0 w-36 h-36 rounded-2xl flex items-center justify-center overflow-hidden bg-surface-100 dark:bg-surface-800 shadow-lg border border-surface-200 dark:border-surface-700">
              @if (imageUrl()) {
                <p-image
                  [src]="(imageUrl()! | resolveApiUrl)!"
                  [alt]="achievementName()"
                  imageClass="max-w-full max-h-full object-contain"
                ></p-image>
              } @else {
                <i class="pi pi-verified text-6xl text-primary-500"></i>
              }
            </div>

            <!-- Details -->
            <div class="flex flex-col gap-3 flex-1 text-center md:text-left">
              <h2 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ achievementName() }}</h2>
              <p class="text-surface-600 dark:text-surface-400 m-0 leading-relaxed">{{ achievementDescription() }}</p>

              <div class="flex flex-wrap gap-2 justify-center md:justify-start mt-2">
                <div class="inline-flex items-center gap-2 bg-primary-50 dark:bg-primary-900/20 text-primary-700 dark:text-primary-300 px-3 py-1.5 rounded-full text-sm font-medium border border-primary-200 dark:border-primary-700">
                  <i class="pi pi-users text-xs"></i>
                  <span>{{ ls.translate('earned_by_count', { count: earners().length }) }}</span>
                </div>
              </div>
            </div>
          </div>
        </p-card>

        <!-- Earners Section -->
        <p-card>
          <ng-template pTemplate="header">
            <div class="px-5 pt-5 pb-0">
              <h3 class="text-lg font-bold m-0 flex items-center gap-2 text-surface-900 dark:text-surface-0">
                <i class="pi pi-trophy text-primary-500"></i>
                {{ ls.t()['earners'] }}
                <p-tag [value]="earners().length.toString()" severity="success" styleClass="ml-2" />
              </h3>
            </div>
          </ng-template>

          @if (earners().length === 0) {
            <div class="flex flex-col items-center justify-center p-12 text-surface-500">
              <i class="pi pi-lock text-5xl mb-4 opacity-20"></i>
              <p class="m-0 font-medium">{{ ls.t()['no_earners'] }}</p>
            </div>
          } @else {
            <div class="grid grid-cols-12 gap-3">
              @for (earner of sortedEarners(); track earner.id) {
                <div class="col-span-12 sm:col-span-6 lg:col-span-4">
                  <div class="earner-card flex items-center gap-3 p-3 rounded-xl bg-surface-50 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 hover:border-primary-300 dark:hover:border-primary-700 transition-all duration-200">

                    <!-- Avatar -->
                    <p-avatar
                      [image]="(earner.userProfilePictureUrl | resolveApiUrl) || undefined"
                      [label]="!earner.userProfilePictureUrl ? (earner.userFullName || earner.userName || '?').substring(0, 1).toUpperCase() : undefined"
                      shape="circle"
                      size="large"
                      styleClass="ring-2 ring-primary-200 dark:ring-primary-800 shadow-sm flex-shrink-0"
                    ></p-avatar>

                    <!-- Info -->
                    <div class="flex flex-col gap-0.5 min-w-0">
                      <span class="font-semibold text-surface-900 dark:text-surface-0 truncate text-sm">
                        {{ earner.userFullName || earner.userName || 'Onbekend' }}
                      </span>
                      @if (earner.userName && earner.userFullName) {
                        <span class="text-xs text-surface-500 truncate">@{{ earner.userName }}</span>
                      }
                      <div class="flex items-center gap-1 mt-0.5">
                        <i class="pi pi-calendar text-xs text-primary-400"></i>
                        <span class="text-xs text-surface-500">{{ earner.dateAdded | date:'d MMM yyyy' }}</span>
                      </div>
                    </div>

                  </div>
                </div>
              }
            </div>
          }
        </p-card>

      } @else {
        <!-- Not found / empty state -->
        <div class="flex items-center gap-3">
          <p-button icon="pi pi-arrow-left" [text]="true" severity="secondary" routerLink="/achievements" />
          <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">Achievement niet gevonden</h1>
        </div>
        <div class="flex flex-col items-center justify-center p-12 text-surface-500 bg-surface-50 dark:bg-surface-800 rounded-xl border border-dashed border-surface-300 dark:border-surface-600">
          <i class="pi pi-trophy text-6xl mb-4 opacity-20"></i>
          <p class="m-0">Dit achievement bestaat niet of is niet gevonden.</p>
        </div>
      }

    </div>
  `,
  styles: [`
    .earner-card {
      cursor: default;
    }
    .achievement-badge {
      box-shadow: 0 4px 24px -4px var(--p-primary-color, rgba(0,0,0,0.15));
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class AchievementDetailComponent implements OnInit {
  private readonly achievementService = inject(AchievementService);
  private readonly authService = inject(AuthService);
  private readonly activatedRoute = inject(ActivatedRoute);
  ls = inject(LanguageService);

  earners = signal<AchievementEntry[]>([]);
  loading = signal<boolean>(true);
  notFound = signal<boolean>(false);

  // Achievement meta loaded from the personal listing
  achievementName = signal<string>('');
  achievementDescription = signal<string>('');
  imageUrl = signal<string | null>(null);

  sortedEarners = computed(() =>
    [...this.earners()].sort((a, b) =>
      new Date(a.dateAdded).getTime() - new Date(b.dateAdded).getTime()
    )
  );

  ngOnInit(): void {
    const id = this.activatedRoute.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      this.notFound.set(true);
      return;
    }
    this.loadData(id);
  }

  private loadData(achievementId: string): void {
    this.loading.set(true);
    const currentUser = this.authService.currentUser();

    forkJoin({
      earners: this.achievementService.getEarnersForAchievement(achievementId),
      listing: currentUser
        ? this.achievementService.getAllForUser(currentUser.id)
        : of<AchievementListing[]>([])
    }).subscribe({
      next: ({ earners, listing }) => {
        this.earners.set(earners);

        // Populate achievement meta from the listing (covers 0-earner case)
        const match = listing.find(a => a.id === achievementId);
        if (match) {
          this.achievementName.set(match.name);
          this.achievementDescription.set(match.description);
          this.imageUrl.set(match.imageUrl ?? null);
        } else if (earners.length > 0) {
          // Fallback: get meta from first earner entry
          this.achievementName.set(earners[0].achievementName);
          this.achievementDescription.set(earners[0].achievementDescription);
          this.imageUrl.set(earners[0].imageUrl ?? null);
        } else {
          this.notFound.set(true);
        }

        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notFound.set(true);
      }
    });
  }
}

