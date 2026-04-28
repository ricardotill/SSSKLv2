import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { QuoteService } from '../../services/quote.service';
import { QuoteDto } from '../../../../core/models/quote.model';
import { LanguageService } from '../../../../core/services/language.service';
import { ReactionListComponent } from '../../../../shared/components/reaction-list/reaction-list.component';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { ResolveApiUrlPipe } from '../../../../shared/pipes/resolve-api-url.pipe';
import { TooltipModule } from 'primeng/tooltip';
import { UserProfileDrawerService } from '../../../../core/services/user-profile-drawer.service';

@Component({
  selector: 'app-quote-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactionListComponent,
    ButtonModule,
    AvatarModule,
    ResolveApiUrlPipe,
    TooltipModule
  ],
  template: `
    <div class="max-w-4xl mx-auto p-4 md:p-6">
      <!-- Back Button -->
      <p-button 
        [label]="ls.t().back" 
        icon="pi pi-arrow-left" 
        [text]="true" 
        routerLink="/quotes" 
        class="mb-6"
      ></p-button>

      @if (loading()) {
        <div class="flex justify-center items-center py-20">
          <i class="pi pi-spin pi-spinner text-4xl text-primary-500"></i>
        </div>
      } @else if (quote(); as q) {
        <div class="flex flex-col gap-8">
          <!-- Quote Content -->
          <div class="bg-surface-0 dark:bg-surface-900 p-8 md:p-12 rounded-3xl shadow-xl border border-surface-200 dark:border-surface-800 relative overflow-hidden">
            <i class="pi pi-quote-left text-8xl text-primary-500/10 absolute -top-4 -left-4 z-0"></i>
            
            <div class="relative z-10 flex flex-col gap-8">
              <p class="text-3xl md:text-4xl font-serif italic font-medium leading-tight text-surface-900 dark:text-surface-0">
                "{{ q.text }}"
              </p>

              <div class="flex flex-wrap items-center gap-4 pt-6 border-t border-surface-100 dark:border-surface-800">
                <span class="text-xl text-surface-400">—</span>
                <div class="flex flex-wrap gap-4">
                  @for (author of q.authors; track author.id) {
                    <div class="flex items-center gap-3">
                      @if (author.applicationUserId) {
                        <p-avatar 
                          [image]="(author.applicationUser?.profilePictureUrl | resolveApiUrl) || undefined" 
                          [label]="!author.applicationUser?.profilePictureUrl ? author.applicationUser?.fullName?.substring(0,1) : undefined"
                          shape="circle" 
                          size="large"
                          class="cursor-pointer ring-2 ring-primary-500/20"
                          (click)="drawerService.open(author.applicationUserId)"
                        ></p-avatar>
                        <div class="flex flex-col">
                          <span 
                            class="text-lg font-bold text-surface-900 dark:text-surface-0 cursor-pointer hover:text-primary-500 transition-colors"
                            (click)="drawerService.open(author.applicationUserId)"
                          >
                            {{ author.applicationUser?.fullName }}
                          </span>
                          <span class="text-xs text-surface-500">@{{ author.applicationUser?.userName }}</span>
                        </div>
                      } @else {
                        <span class="text-lg font-bold text-surface-900 dark:text-surface-0">
                          {{ author.customName }}
                        </span>
                      }
                    </div>
                  }
                </div>
              </div>

              <div class="flex justify-between items-center text-sm text-surface-500">
                <div class="flex items-center gap-6">
                  <div class="flex items-center gap-2">
                    <i class="pi pi-calendar"></i>
                    <span class="font-medium">{{ q.dateSaid | date:'longDate' }}</span>
                  </div>
                  <!-- Vote Button -->
                  <p-button 
                    [label]="(q.voteCount || 0).toString()" 
                    [icon]="q.hasVoted ? 'pi pi-heart-fill' : 'pi pi-heart'" 
                    [severity]="q.hasVoted ? 'danger' : 'secondary'"
                    [text]="!q.hasVoted"
                    (onClick)="toggleVote()"
                    [pTooltip]="'Stemmen'"
                    rounded="true"
                  ></p-button>
                </div>
                <div class="flex items-center gap-2">
                  <span class="opacity-60">{{ ls.t().added_by }}</span>
                  <span class="font-medium text-surface-700 dark:text-surface-300">{{ q.createdBy?.fullName }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Reactions & Comments Section -->
          <div class="bg-surface-0 dark:bg-surface-900 p-6 md:p-8 rounded-3xl shadow-lg border border-surface-200 dark:border-surface-800">
            <h2 class="text-xl font-bold mb-6 flex items-center gap-2 text-surface-900 dark:text-surface-0">
              <i class="pi pi-comments text-primary-500"></i>
              {{ ls.t().quotes_comments }}
            </h2>

            <app-reaction-list 
              [targetId]="q.id" 
              targetType="Quote"
            ></app-reaction-list>
          </div>
        </div>
      } @else {
        <div class="text-center py-20">
          <p class="text-xl text-surface-500">Quote niet gevonden.</p>
          <p-button [label]="ls.t().back" routerLink="/quotes" severity="secondary" [text]="true"></p-button>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
  `]
})
export class QuoteDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private quoteService = inject(QuoteService);
  public ls = inject(LanguageService);
  public drawerService = inject(UserProfileDrawerService);

  quote = signal<QuoteDto | null>(null);
  loading = signal(true);

  ngOnInit(): void {
    this.loadQuote();
  }

  loadQuote(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.quoteService.getQuote(id).subscribe({
        next: (q: QuoteDto) => {
          this.quote.set(q);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
        }
      });
    } else {
      this.loading.set(false);
    }
  }

  toggleVote(): void {
    const q = this.quote();
    if (!q) return;

    this.quoteService.toggleVote(q.id).subscribe(() => {
      this.loadQuote();
    });
  }
}
