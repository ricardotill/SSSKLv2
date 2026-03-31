import { Component, ChangeDetectionStrategy, inject, signal, OnInit, viewChild } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { TableModule, TableLazyLoadEvent, Table } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { OrderService } from './services/order.service';
import { OrderDto } from '../../core/models/order.model';
import { LanguageService } from '../../core/services/language.service';
import { AuthService } from '../../core/auth/auth.service';
import { finalize } from 'rxjs';

import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-personal-orders',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, DatePipe, CurrencyPipe, ConfirmDialogModule, CardModule],
  providers: [ConfirmationService],
  template: `
    <p-confirmDialog />
    
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().my_orders }}</h1>
      <p-button icon="pi pi-refresh" [rounded]="true" [text]="false" (onClick)="refreshTable()" [ariaLabel]="ls.t().refresh"></p-button>
    </div>

    <p-card>
      <p-table
        stripedRows
        #dt
        [value]="orders()"
        [lazy]="true"
        (onLazyLoad)="loadOrders($event)"
        [paginator]="true"
        [rows]="15"
        [totalRecords]="totalRecords()"
        [loading]="loading()"
        [rowsPerPageOptions]="[15, 30, 50]"
        [showCurrentPageReport]="true"
        [currentPageReportTemplate]="ls.translate('showing_orders_report', { first: '{first}', last: '{last}', total: '{totalRecords}' })"
        responsiveLayout="scroll"
      >
        <ng-template pTemplate="header">
          <tr>
            <th>{{ ls.t().date }}</th>
            <th>{{ ls.t().product }}</th>
            <th>{{ ls.t().amount }}</th>
            <th>{{ ls.t().price }}</th>
            <th class="w-32">{{ ls.t().actions }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-order>
          <tr>
            <td>{{ order.createdOn | date: 'dd-MM-yyyy HH:mm' }}</td>
            <td>{{ order.productName }}</td>
            <td>{{ order.amount }}x</td>
            <td class="font-medium text-primary">{{ order.paid | currency: 'EUR' }}</td>
            <td>
              <p-button
                icon="pi pi-trash"
                [rounded]="true"
                [text]="true"
                severity="danger"
                [loading]="deletingOrderId() === order.id"
                (onClick)="onDeleteClick(order)"
                [ariaLabel]="ls.t().delete"
              />
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="5" class="text-center p-8 text-surface-400">{{ ls.t().no_orders }}</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class PersonalOrdersComponent implements OnInit {
  private orderService = inject(OrderService);
  private confirmationService = inject(ConfirmationService);
  private messageService = inject(MessageService);
  private authService = inject(AuthService);
  ls = inject(LanguageService);

  dt = viewChild<Table>('dt');

  orders = signal<OrderDto[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  deletingOrderId = signal<string | null>(null);

  ngOnInit() {
  }

  loadOrders(event: TableLazyLoadEvent) {
    this.loading.set(true);
    const skip = event.first ?? 0;
    const take = event.rows ?? 15;

    this.orderService.getPersonalOrders(skip, take)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.orders.set(response.items);
          this.totalRecords.set(response.totalCount);
        },
        error: (err) => {
          console.error('Failed to load personal orders', err);
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        }
      });
  }

  onDeleteClick(order: OrderDto) {
    this.confirmationService.confirm({
      message: this.ls.translate('confirm_delete_order', { product: order.productName }),
      header: this.ls.t().confirm_delete_title,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteOrder(order.id);
      }
    });
  }

  private deleteOrder(id: string) {
    this.deletingOrderId.set(id);
    this.orderService.deleteOrder(id)
      .pipe(finalize(() => this.deletingOrderId.set(null)))
      .subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().confirm, detail: this.ls.t().order_deleted });
          this.authService.refreshCurrentUser();
          this.refreshTable();
        },
        error: (err) => {
          console.error('Failed to delete order', err);
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().error });
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
