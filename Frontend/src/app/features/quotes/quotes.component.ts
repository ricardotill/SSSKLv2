import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
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

      <p-dataView 
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
      </p-dataView>
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
  }

  loadSettings(): void {
    this.settingsService.getSetting('QuotesFeatureAllowedRoles').subscribe({
      next: (setting) => {
        if (setting.value) {
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
        // Backend doesn't return PaginationObject for Quotes yet, just QuoteDto[]
        // I'll adjust the backend or frontend later if needed.
        // For now let's assume it returns QuoteDto[] as per current implementation.
        this.quotes.set(response);
        this.totalRecords.set(response.length); // Temporary
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

  openQuoteForm(quote?: QuoteDto): void {
    const ref = this.dialogService.open(QuoteFormDialogComponent, {
      header: quote ? 'Edit Quote' : 'Add Quote',
      width: '500px',
      data: { quote }
    });

    if (ref) {
      ref.onClose.subscribe((result: any) => {
        if (result) {
          if (quote) {
            this.quoteService.updateQuote(quote.id, result).subscribe({
              next: () => {
                this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().quote_updated_success });
                this.loadQuotes();
              },
              error: () => this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().update_failed })
            });
          } else {
            this.quoteService.createQuote(result).subscribe({
              next: () => {
                this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().quote_added_success });
                this.loadQuotes();
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
