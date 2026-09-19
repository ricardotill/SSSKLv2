import { CommonModule, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../core/auth/auth.service';
import { UserStat } from '../../core/models/user-stat.model';
import { AchievementEntry } from '../../core/models/achievement.model';
import { LanguageService } from '../../core/services/language.service';
import { AchievementService } from '../achievements/services/achievement.service';
import { ApplicationUserService } from '../users/services/application-user.service';
import { ResolveApiUrlPipe } from '../../shared/pipes/resolve-api-url.pipe';

@Component({
  selector: 'app-stats',
  standalone: true,
  imports: [CommonModule, DatePipe, DecimalPipe, ProgressSpinnerModule, RouterModule, TagModule, ButtonModule, ResolveApiUrlPipe],
  template: `
    <div class="max-w-6xl mx-auto flex flex-col gap-6">
      <header class="flex flex-col gap-1">
        <span class="text-sm font-semibold uppercase tracking-wider text-primary">{{ viewingOwnStats() ? 'Mijn profiel' : 'Gebruikersprofiel' }}</span>
        <h1 class="text-3xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ viewingOwnStats() ? 'Mijn statistieken' : 'Statistieken van ' + (displayedUserName() ?? 'gebruiker') }}</h1>
        <p class="text-surface-500 m-0">{{ viewingOwnStats() ? 'Een volledig overzicht van jouw activiteit op SSSKL.' : 'Een volledig overzicht van de activiteit van deze gebruiker op SSSKL.' }}</p>
        <div class="flex items-center gap-3 mt-3">
          <p-button
            label="Statistieken herberekenen"
            icon="pi pi-refresh"
            size="small"
            [loading]="recalculating()"
            [disabled]="!canRecalculate()"
            (onClick)="recalculateStats()"
          ></p-button>
          @if (stats()?.lastStatsRecalculatedAt; as recalculatedAt) {
            <span class="text-sm text-surface-500">Laatste herberekening: {{ recalculatedAt | date:'medium' }}</span>
          }
        </div>
      </header>

      @if (loading()) {
        <div class="flex justify-center items-center p-16">
          <p-progressSpinner ariaLabel="Laden"></p-progressSpinner>
        </div>
      } @else if (stats(); as s) {
        <section class="grid grid-cols-2 md:grid-cols-4 gap-3">
          @for (item of highlights(s); track item.label) {
            <div class="p-4 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900 shadow-sm">
              <div class="flex items-center justify-between gap-2 text-surface-500">
                <span class="text-sm">{{ item.label }}</span>
                <i [class]="'pi ' + item.icon + ' text-primary'"></i>
              </div>
              <strong class="block mt-2 text-2xl text-surface-900 dark:text-surface-0">{{ item.value }}</strong>
            </div>
          }
        </section>

        <section class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div class="p-5 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900">
            <h2 class="text-xl font-semibold mt-0 mb-5 flex items-center gap-2"><i class="pi pi-shopping-cart text-primary"></i>Bestellingen en saldo</h2>
            <div class="stat-list">
              <div><span>Totale bestellingen</span><strong>{{ s.totalOrders | number }}</strong></div>
              <div><span>Gekochte items</span><strong>{{ s.totalItemsBought | number }}</strong></div>
              <div><span>Totaal besteed</span><strong>{{ s.totalSpent | number:'1.2-2' }}</strong></div>
              <div><span>Totale opwaardering</span><strong>{{ s.totalTopUp | number:'1.2-2' }}</strong></div>
              <div><span>Grootste losse opwaardering</span><strong>{{ s.maxSingleTopUp | number:'1.2-2' }}</strong></div>
              <div><span>Meeste bestellingen per uur</span><strong>{{ s.maxOrdersPerHour | number }}</strong></div>
            </div>
          </div>

          <div class="p-5 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900">
            <h2 class="text-xl font-semibold mt-0 mb-5 flex items-center gap-2"><i class="pi pi-users text-primary"></i>Community</h2>
            <div class="stat-list">
              <div><span>Quotes geplaatst</span><strong>{{ s.quoteCount | number }}</strong></div>
              <div><span>Quotes gestemd</span><strong>{{ s.quoteVotesGiven | number }}</strong></div>
              <div><span>Ontvangen quote-stemmen</span><strong>{{ s.quoteVotesReceived | number }}</strong></div>
              <div><span>Reacties geplaatst</span><strong>{{ s.reactionCount | number }}</strong></div>
              <div><span>Huidige streak</span><strong>{{ s.currentStreak | number }} dagen</strong></div>
              <div><span>Lid sinds</span><strong>{{ s.membershipStartDate | date:'mediumDate' }}</strong></div>
            </div>
          </div>

          <div class="p-5 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900">
            <h2 class="text-xl font-semibold mt-0 mb-5 flex items-center gap-2"><i class="pi pi-stopwatch text-primary"></i>Persoonlijke records</h2>
            <div class="stat-list">
              <div><span>Kortste tijd tussen bestellingen</span><strong>{{ minutes(s.minMinutesBetweenOrders) }}</strong></div>
              <div><span>Kortste tijd tussen opwaarderingen</span><strong>{{ minutes(s.minMinutesBetweenTopUp) }}</strong></div>
              <div><span>Laatste activiteit</span><strong>{{ s.lastActivityDate | date:'medium' }}</strong></div>
              <div><span>Laatste bestelling</span><strong>{{ s.lastOrderDate | date:'medium' }}</strong></div>
              <div><span>Laatste opwaardering</span><strong>{{ s.lastTopUpDate | date:'medium' }}</strong></div>
            </div>
          </div>

          <div class="p-5 rounded-xl border border-surface-200 dark:border-surface-700 bg-surface-0 dark:bg-surface-900">
            <div class="flex items-center justify-between gap-3 mb-5">
              <h2 class="text-xl font-semibold m-0 flex items-center gap-2"><i class="pi pi-trophy text-primary"></i>Behaalde achievements</h2>
              <a routerLink="/achievements" class="text-primary text-sm font-medium">Alles bekijken</a>
            </div>
            @if (achievements().length) {
              <div class="flex flex-wrap gap-3">
                @for (achievement of achievements(); track achievement.id) {
                  <a [routerLink]="['/achievements', achievement.achievementId]" class="achievement flex items-center gap-2 p-2 rounded-lg border border-surface-200 dark:border-surface-700 hover:border-primary transition-colors">
                    @if (achievement.imageUrl) {
                      <img [src]="achievement.imageUrl | resolveApiUrl" [alt]="achievement.achievementName" class="w-10 h-10 object-contain" />
                    } @else {
                      <i class="pi pi-verified text-2xl text-primary"></i>
                    }
                    <span class="text-sm font-medium">{{ achievement.achievementName }}</span>
                  </a>
                }
              </div>
            } @else {
              <p class="text-surface-500 m-0">Nog geen achievements behaald.</p>
            }
          </div>
        </section>
      } @else {
        <p class="text-surface-500">Statistieken konden niet worden geladen.</p>
      }
    </div>
  `,
  styles: [`
    .stat-list { display: flex; flex-direction: column; gap: .75rem; }
    .stat-list > div { display: flex; justify-content: space-between; gap: 1rem; padding-bottom: .75rem; border-bottom: 1px solid var(--p-surface-200); }
    .stat-list > div:last-child { border-bottom: 0; padding-bottom: 0; }
    .stat-list span { color: var(--p-text-muted-color); }
    .stat-list strong { text-align: right; color: var(--p-text-color); }
    .achievement { color: var(--p-text-color); text-decoration: none; }
    @media (prefers-color-scheme: dark) { .stat-list > div { border-color: var(--p-surface-700); } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatsComponent {
  private readonly authService = inject(AuthService);
  private readonly userService = inject(ApplicationUserService);
  private readonly messageService = inject(MessageService);
  private readonly achievementService = inject(AchievementService);
  private readonly route = inject(ActivatedRoute);
  private readonly requestedUserId = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('userId'))),
    { initialValue: this.route.snapshot.queryParamMap.get('userId') }
  );
  readonly ls = inject(LanguageService);
  readonly stats = signal<UserStat | null>(null);
  readonly achievements = signal<AchievementEntry[]>([]);
  readonly loading = signal(true);
  readonly recalculating = signal(false);
  readonly viewingOwnStats = signal(true);
  readonly displayedUserName = signal<string | null>(null);
  private readonly displayedUserId = signal<string | null>(null);

  constructor() {
    effect(() => {
      const user = this.authService.currentUser();
      if (!user) return;

      const requestedUserId = this.requestedUserId();
      const isAdmin = user.roles?.includes('Admin') ?? false;
      const userId = isAdmin && requestedUserId ? requestedUserId : user.id;
      this.displayedUserId.set(userId);
      this.viewingOwnStats.set(userId === user.id);
      this.load(userId, userId !== user.id);
    });
  }

  private load(userId: string, loadUserName: boolean): void {
    this.loading.set(true);
    this.displayedUserName.set(null);
    this.userService.getUserStats(userId).subscribe({ next: stats => { this.stats.set(stats); this.loading.set(false); }, error: () => this.loading.set(false) });
    this.achievementService.getAchievementEntries(userId).subscribe({ next: entries => this.achievements.set(entries), error: () => this.achievements.set([]) });
    if (loadUserName) {
      this.userService.getUser(userId).subscribe({ next: user => this.displayedUserName.set(user.fullName) });
    }
  }

  canRecalculate(): boolean {
    if (this.authService.currentUser()?.roles?.includes('Admin')) return true;

    const recalculatedAt = this.stats()?.lastStatsRecalculatedAt;
    return !recalculatedAt || Date.now() - new Date(recalculatedAt).getTime() >= 7 * 24 * 60 * 60 * 1000;
  }

  recalculateStats(): void {
    const userId = this.displayedUserId();
    if (!userId || !this.canRecalculate()) return;

    this.recalculating.set(true);
    this.userService.recalculateUserStats(userId).subscribe({
      next: stats => {
        this.stats.set(stats);
        this.recalculating.set(false);
        this.messageService.add({ severity: 'success', summary: 'Statistieken bijgewerkt', detail: 'Jouw statistieken zijn herberekend.' });
      },
      error: response => {
        this.recalculating.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Herberekening mislukt',
          detail: response.status === 429 ? 'Je kunt statistieken maar eenmaal per zeven dagen herberekenen.' : 'Statistieken konden niet worden herberekend.'
        });
      }
    });
  }

  highlights(stats: UserStat): { label: string; value: string | number; icon: string }[] {
    return [
      { label: 'Bestellingen', value: stats.totalOrders, icon: 'pi-shopping-cart' },
      { label: 'Uitgegeven', value: stats.totalSpent.toFixed(2), icon: 'pi-wallet' },
      { label: 'Streak', value: `${stats.currentStreak} dagen`, icon: 'pi-bolt' },
      { label: 'Achievements', value: this.achievements().length, icon: 'pi-trophy' }
    ];
  }

  minutes(value: number): string {
    return value === 2147483647 ? 'Geen gegevens' : `${value} min.`;
  }
}
