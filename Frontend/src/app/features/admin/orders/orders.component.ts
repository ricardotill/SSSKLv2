import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { finalize } from 'rxjs';
import { OrderService } from '../../../core/services/order.service';
import { OrderDto } from '../../../core/models/order.model';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [
    DatePipe,
    CurrencyPipe,
    TableModule,
    ButtonModule,
    CardModule,
    ToastModule,
    ConfirmDialogModule
  ],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().admin_orders }}</h1>
      <p-button icon="pi pi-refresh" [rounded]="true" (onClick)="loadOrders()" [ariaLabel]="ls.t().refresh"></p-button>
    </div>
    <p-toast></p-toast>
    <p-confirmDialog></p-confirmDialog>
    <p-card>
      <p-table 
        stripedRows
        [value]="orders()" 
        [loading]="loading()" 
        [paginator]="true" 
        [rows]="15" 
        [totalRecords]="totalRecords()"
        [lazy]="true" 
        (onLazyLoad)="onLazyLoad($event)"
        responsiveLayout="scroll"
        [showCurrentPageReport]="true"
        [currentPageReportTemplate]="ls.t().showing_orders_report_no_total"
        [rowsPerPageOptions]="[10, 15, 25, 50]">
        <ng-template pTemplate="header">
          <tr>
            <th>{{ ls.t().date }}</th>
            <th>{{ ls.t().user }}</th>
            <th>{{ ls.t().product }}</th>
            <th>{{ ls.t().price }}</th>
            <th class="w-32">{{ ls.t().actions }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-order>
          <tr>
            <td>{{ order.createdOn | date:'dd-MM-yyyy HH:mm' }}</td>
            <td>{{ order.userFullName }}</td>
            <td>{{ order.productName }}</td>
            <td>{{ order.amount | currency:'EUR' }}</td>
            <td>
              <div class="flex gap-2">
                <p-button icon="pi pi-trash" [rounded]="true" [text]="true" severity="danger" (onClick)="confirmDelete(order)" [loading]="deletingOrderId() === order.id" [ariaLabel]="ls.t().delete"></p-button>
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="5" class="text-center p-4 text-surface-500">{{ ls.t().no_orders }}</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [MessageService, ConfirmationService]
})
export default class OrdersComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  ls = inject(LanguageService);

  orders = signal<OrderDto[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  deletingOrderId = signal<string | null>(null);

  skip = 0;
  take = 15;

  ngOnInit(): void {
    // Initial load will be handled by onLazyLoad on table initialization if needed, 
    // but we can call it here too if not using lazy load correctly. 
    // With [lazy]="true", onLazyLoad is called on init.
  }

  loadOrders(): void {
    this.loading.set(true);
    this.orderService.getOrders(this.skip, this.take).subscribe({
      next: (data) => {
        this.orders.set(data.items);
        this.totalRecords.set(data.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        this.loading.set(false);
      }
    });
  }

  onLazyLoad(event: any): void {
    this.skip = event.first || 0;
    this.take = event.rows || 15;
    this.loadOrders();
  }

  confirmDelete(order: OrderDto): void {
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

  deleteOrder(id: string): void {
    this.deletingOrderId.set(id);
    this.orderService.deleteOrder(id)
      .pipe(finalize(() => this.deletingOrderId.set(null)))
      .subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().order_deleted });
          this.loadOrders();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().delete_failed });
        }
      });
  }
}
