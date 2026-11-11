import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DataViewModule, DataViewLazyLoadEvent } from 'primeng/dataview';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MessageService } from 'primeng/api';
import { QuoteService } from './services/quote.service';
import { QuoteDto, QuoteCreateDto } from '../../core/models/quote.model';
import { LanguageService } from '../../core/services/language.service';
import { AuthService } from '../../core/auth/auth.service';
import { GlobalSettingsService } from '../admin/services/global-settings.service';
import { QuoteCardComponent } from './components/quote-card/quote-card.component';
import { QuoteFormDialogComponent } from './components/quote-form-dialog/quote-form-dialog.component';
import { EmbeddedService } from '../../core/services/embedded.service';
import { FROM_APP_STATE } from '../../core/services/native-bridge.service';

@Component({
  selector: 'app-quotes',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    DataViewModule,
    ProgressSpinnerModule,
    QuoteCardComponent
  ],
  providers: [DialogService],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h1 class="text-3xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().quotes }}</h1>
          <p class="text-surface-500 m-0">{{ ls.t().quotes_desc }}</p>
        </div>
        <div class="flex items-center gap-3">
          @if (canCreate()) {
            <p-button 
              [label]="ls.t().add_quote" 
              icon="pi pi-plus" 
              (onClick)="openQuoteForm()" 
              severity="primary" 
            />
          }
        </div>
      </div>

      <p-data-view 
        [value]="quotes()" 
        layout="grid" 
        [paginator]="true" 
        paginatorStyleClass="mt-6"
        [rows]="rows" 
        [totalRecords]="totalRecords()"
        [lazy]="true"
        (onLazyLoad)="onLazyLoad($event)"
        [loading]="loading()"
      >
        <ng-template #grid let-items>
          <div class="grid grid-cols-12 gap-6">
            @for (quote of items; track quote.id) {
              <div class="col-span-12 md:col-span-6 xl:col-span-4">
                <app-quote-card 
                  [quote]="quote"
                  [canEdit]="canEdit(quote)"
                  [canDelete]="canDelete(quote)"
                  (onEdit)="openQuoteForm($event)"
                  (onDelete)="confirmDelete($event)"
                  (onVote)="loadQuotes()"
                ></app-quote-card>
              </div>
            }
          </div>
        </ng-template>

        <ng-template #emptymessage>
          <div class="flex flex-col items-center justify-center p-12 text-surface-500 bg-surface-50 dark:bg-surface-800/50 rounded-xl border border-dashed border-surface-300 dark:border-surface-600 w-full">
            <i class="pi pi-comment text-6xl mb-4 opacity-20"></i>
            <p class="text-lg font-medium">{{ ls.t().no_quotes_found }}</p>
            @if (canCreate()) {
              <p-button [label]="ls.t().be_the_first_quote" [text]="true" (onClick)="openQuoteForm()" />
            }
          </div>
        </ng-template>
      </p-data-view>
    </div>
  `,
  styles: [`
    :host ::ng-deep .p-dataview-content {
      background: transparent !important;
      border: none !important;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class QuotesComponent implements OnInit {
  private readonly quoteService = inject(QuoteService);
  private readonly authService = inject(AuthService);
  private readonly dialogService = inject(DialogService);
  private readonly messageService = inject(MessageService);
  private readonly settingsService = inject(GlobalSettingsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly embedded = inject(EmbeddedService);
  ls = inject(LanguageService);

  quotes = signal<QuoteDto[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  
  allowedRoles = signal<string[]>([]);
  
  canCreate = computed(() => {
    const user = this.authService.currentUser() as any;
    if (!user) return false;
    if (user.roles?.includes('Admin')) return true;
    
    const roles = this.allowedRoles();
    if (roles.length === 0) return true; // Default to open if not configured?
    
    return roles.some(r => user.roles.includes(r));
  });

  rows = 12;
  first = 0;

  ngOnInit(): void {
    this.loadSettings();
    this.openFormFromLink();
  }

  /**
   * `?new` opens the form for a new quote, `?edit=<id>` the form for that quote. The mobile app opens these
   * links in its WebView (its quote pages are native) and gets the result handed back afterwards.
   */
  private openFormFromLink(): void {
    const params = this.route.snapshot.queryParamMap;
    if (params.has('new')) {
      this.openQuoteForm(undefined, true);
      return;
    }
    const editId = params.get('edit');
    if (editId) {
      this.quoteService.getQuote(editId).subscribe({
        next: (quote) => this.openQuoteForm(quote, true),
        error: () => this.finishLinkedForm('/quotes'),
      });
    }
  }

  /**
   * After a form opened from a link: in the mobile app, hand back to its native page for `path`. The URL
   * is reset first (marked as the app's own navigation, so it isn't handed over again), so the app's next
   * `?new` or `?edit=` link opens the form again in the reused WebView.
   */
  private finishLinkedForm(path: string): void {
    if (!this.embedded.isEmbedded) {
      this.loadQuotes();
      return;
    }
    void this.router.navigate(['/quotes'], { replaceUrl: true, state: { [FROM_APP_STATE]: true } });
    this.embedded.notifyNative('open', path);
  }

  loadSettings(): void {
    this.settingsService.getSetting('QuotesFeatureAllowedRoles').subscribe({
      next: (setting) => {
        if (setting?.value) {
          this.allowedRoles.set(setting.value.split(',').map(r => r.trim()));
        }
      },
      error: () => console.log('Quotes feature roles setting not found, using defaults.')
    });
  }

  onLazyLoad(event: DataViewLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? 12;
    this.loadQuotes();
  }

  loadQuotes(): void {
    this.loading.set(true);
    this.quoteService.getQuotes(this.first, this.rows).subscribe({
      next: (response) => {
        this.quotes.set(response.items);
        this.totalRecords.set(response.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  canEdit(quote: QuoteDto): boolean {
    const user = this.authService.currentUser() as any;
    if (!user) return false;
    if (user.roles?.includes('Admin')) return true;
    if (quote.createdBy?.id === user.id) return true;
    return quote.authors.some((a: any) => a.applicationUserId === user.id);
  }

  canDelete(quote: QuoteDto): boolean {
    return this.canEdit(quote);
  }

  /** @param fromLink opened by `?new` / `?edit=`: hand the result back to the mobile app when it's done. */
  openQuoteForm(quote?: QuoteDto, fromLink = false): void {
    const ref = this.dialogService.open(QuoteFormDialogComponent, {
      header: quote ? 'Edit Quote' : 'Add Quote',
      width: '500px',
      data: { quote }
    });

    if (ref) {
      ref.onClose.subscribe((result: any) => {
        const backTo = quote ? `/quotes/${quote.id}` : '/quotes';
        if (!result && fromLink) {
          this.finishLinkedForm(backTo);
        }
        if (result) {
          if (quote) {
            this.quoteService.updateQuote(quote.id, result).subscribe({
              next: () => {
                this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().quote_updated_success });
                if (fromLink) {
                  this.finishLinkedForm(backTo);
                } else {
                  this.loadQuotes();
                }
              },
              error: () => this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().update_failed })
            });
          } else {
            this.quoteService.createQuote(result).subscribe({
              next: (created) => {
                this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().quote_added_success });
                if (fromLink) {
                  this.finishLinkedForm(created?.id ? `/quotes/${created.id}` : '/quotes');
                } else {
                  this.loadQuotes();
                }
              },
              error: () => this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().update_failed })
            });
          }
        }
      });
    }
  }

  confirmDelete(quote: QuoteDto): void {
    // For simplicity, I'll just delete directly or use a confirm dialog if available
    if (confirm(this.ls.t().confirm_delete_quote)) {
      this.quoteService.deleteQuote(quote.id).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().quote_deleted_success });
          this.loadQuotes();
        },
        error: () => this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().delete_failed })
      });
    }
  }
}
