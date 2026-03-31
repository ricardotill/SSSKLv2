import { Component, ChangeDetectionStrategy, inject, signal, viewChild } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { TableModule, TableLazyLoadEvent, Table } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageService } from 'primeng/api';
import { TopUpService } from './services/top-up.service';
import { TopUpDto } from '../../core/models/top-up.model';
import { LanguageService } from '../../core/services/language.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-personal-top-ups',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, CardModule, CurrencyPipe, DatePipe],
  template: `
    
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().my_saldo }}</h1>
      <p-button icon="pi pi-refresh" [rounded]="true" [text]="false" (onClick)="refreshTable()" [ariaLabel]="ls.t().refresh"></p-button>
    </div>

    <p-card>
      <p-table
        stripedRows
        #dt
        [value]="topUps()"
        [lazy]="true"
        (onLazyLoad)="loadTopUps($event)"
        [paginator]="true"
        [rows]="15"
        [totalRecords]="totalRecords()"
        [loading]="loading()"
        [rowsPerPageOptions]="[15, 30, 50]"
        [showCurrentPageReport]="true"
        [currentPageReportTemplate]="ls.translate('showing_top_ups_report', { first: '{first}', last: '{last}', total: '{totalRecords}' })"
        responsiveLayout="scroll"
      >
        <ng-template pTemplate="header">
          <tr>
            <th>{{ ls.t().date }}</th>
            <th>{{ ls.t().amount }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-topUp>
          <tr>
            <td>{{ topUp.createdOn | date: 'dd-MM-yyyy HH:mm' }}</td>
            <td class="font-medium text-primary">{{ topUp.saldo | currency: 'EUR' }}</td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="2" class="text-center p-8 text-surface-400">{{ ls.t().no_top_ups }}</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class PersonalTopUpsComponent {
  private topUpService = inject(TopUpService);
  private messageService = inject(MessageService);
  ls = inject(LanguageService);

  dt = viewChild<Table>('dt');

  topUps = signal<TopUpDto[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);

  loadTopUps(event: TableLazyLoadEvent) {
    this.loading.set(true);
    const skip = event.first ?? 0;
    const take = event.rows ?? 15;

    this.topUpService.getPersonalTopUps(skip, take)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          // Flattening standard paginated response if the backend returns it this way
          // Actually, based on OrderService, it returns an object with items and totalCount
          this.topUps.set(response.items || []);
          this.totalRecords.set(response.totalCount || 0);
        },
        error: (err) => {
          console.error('Failed to load personal topups', err);
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        }
      });
  }

  refreshTable() {
    const table = this.dt();
    if (table) {
      table.onLazyLoad.emit(table.createLazyLoadMetadata());
    }
  }
}
