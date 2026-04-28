import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { QuoteDto } from '../../../../core/models/quote.model';
import { QuoteService } from '../../services/quote.service';
import { LanguageService } from '../../../../core/services/language.service';
import { UserProfileDrawerService } from '../../../../core/services/user-profile-drawer.service';
import { ResolveApiUrlPipe } from '../../../../shared/pipes/resolve-api-url.pipe';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-quote-card',
  standalone: true,
  imports: [
    CommonModule,
    CardModule,
    ButtonModule,
    AvatarModule,
    ResolveApiUrlPipe,
    TooltipModule
  ],
  template: `
    <p-card 
      styleClass="h-full hover:shadow-lg transition-all duration-300 border border-surface-200 dark:border-surface-700 relative overflow-hidden cursor-pointer"
      (click)="navigateToDetail($event)"
    >
      <div class="flex flex-col gap-4 h-full">
        <!-- Quote Text -->
        <div class="relative">
          <i class="pi pi-quote-left text-4xl text-primary-200 dark:text-primary-900/30 absolute -top-2 -left-2 z-0"></i>
          <p class="text-xl font-medium m-0 relative z-10 italic leading-relaxed text-surface-900 dark:text-surface-0 line-clamp-4">
            "{{ quote.text }}"
          </p>
        </div>

        <!-- Authors -->
        <div class="flex flex-wrap items-center gap-2 mt-auto">
          <span class="text-sm text-surface-500 font-medium">—</span>
          @for (author of quote.authors; track author.id; let last = $last) {
            <div class="flex items-center gap-1.5 group">
              @if (author.applicationUserId) {
                <div class="flex items-center gap-2 cursor-pointer group/author" (click)="$event.stopPropagation(); drawerService.open(author.applicationUserId!)">
                  <p-avatar 
                    [image]="(author.applicationUser?.profilePictureUrl | resolveApiUrl) || undefined" 
                    [label]="!author.applicationUser?.profilePictureUrl ? author.applicationUser?.fullName?.substring(0,1) : undefined"
                    shape="circle" 
                    size="normal"
                    [styleClass]="'ring-1 ring-surface-200 dark:ring-surface-700 transition-transform group-hover/author:scale-110'"
                  ></p-avatar>
                  <span class="text-sm font-semibold text-surface-700 dark:text-surface-300 group-hover/author:text-primary-500 transition-colors">
                    {{ author.applicationUser?.fullName }}
                  </span>
                </div>
              } @else {
                <span class="text-sm font-semibold text-surface-700 dark:text-surface-300">
                  {{ author.customName }}
                </span>
              }
              @if (!last) {
                <span class="text-surface-400">&</span>
              }
            </div>
          }
        </div>

        <!-- Footer Info & Actions -->
        <div class="flex justify-between items-center pt-4 border-t border-surface-100 dark:border-surface-800">
          <div class="flex items-center gap-4">
            <!-- Reactions Count -->
            <div 
              class="flex items-center gap-1.5 transition-colors group/vote" 
              [ngClass]="quote.hasVoted ? 'text-red-500' : 'text-surface-500 hover:text-red-500'"
              [pTooltip]="'Stemmen'"
              (click)="toggleVote($event)"
            >
              <i class="pi transition-transform group-hover/vote:scale-125" [ngClass]="quote.hasVoted ? 'pi-heart-fill' : 'pi-heart'"></i>
              <span class="text-sm font-bold">{{ quote.voteCount || 0 }}</span>
            </div>
            <!-- Comments Count -->
            <div class="flex items-center gap-1.5 text-surface-500" [pTooltip]="'Reacties'">
              <i class="pi pi-comments text-primary-500"></i>
              <span class="text-sm font-bold">{{ quote.commentsCount || 0 }}</span>
            </div>
          </div>

          <div class="flex items-center gap-1">
            @if (canEdit) {
              <p-button icon="pi pi-pencil" [text]="true" [rounded]="true" severity="secondary" size="small" (onClick)="$event.stopPropagation(); onEdit.emit(quote)" />
            }
            @if (canDelete) {
              <p-button icon="pi pi-trash" [text]="true" [rounded]="true" severity="danger" size="small" (onClick)="$event.stopPropagation(); onDelete.emit(quote)" />
            }
          </div>
        </div>
      </div>
    </p-card>
  `,
  styles: [`
    :host ::ng-deep .p-card-body {
      padding: 1.5rem;
    }
  `]
})
export class QuoteCardComponent {
  @Input({ required: true }) quote!: QuoteDto;
  @Input() canEdit = false;
  @Input() canDelete = false;
  @Input() showCreator = true;

  @Output() onEdit = new EventEmitter<QuoteDto>();
  @Output() onDelete = new EventEmitter<QuoteDto>();
  @Output() onVote = new EventEmitter<QuoteDto>();

  ls = inject(LanguageService);
  drawerService = inject(UserProfileDrawerService);
  private router = inject(Router);
  private quoteService = inject(QuoteService);

  toggleVote(event: MouseEvent): void {
    event.stopPropagation();
    this.quoteService.toggleVote(this.quote.id).subscribe(() => {
      this.onVote.emit(this.quote);
    });
  }

  navigateToDetail(event: MouseEvent): void {
    // Navigate to detail page
    this.router.navigate(['/quotes', this.quote.id]);
  }
}
