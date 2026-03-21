import { Component, ChangeDetectionStrategy, inject, signal, OnInit, viewChild } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { TableModule, TableLazyLoadEvent, Table } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { ConfirmationService, MessageService } from 'primeng/api';
import { OrderService } from '../../core/services/order.service';
import { OrderDto } from '../../core/models/order.model';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-personal-orders',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, DatePipe, CurrencyPipe, ConfirmDialogModule, ToastModule],
  providers: [ConfirmationService, MessageService],
  template: `
    <p-toast />
    <p-confirmDialog />
    
    <div class="p-6">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-3xl font-bold m-0">My Orders</h1>
      </div>

      <div class="card p-0 overflow-hidden border-round bg-surface-900 border-1 border-surface-700">
        <p-table
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
          currentPageReportTemplate="Showing {first} to {last} of {totalRecords} orders"
          responsiveLayout="scroll"
          styleClass="p-datatable-sm"
        >
          <ng-template pTemplate="header">
            <tr>
              <th class="bg-surface-800 text-surface-0 border-surface-700">Date</th>
              <th class="bg-surface-800 text-surface-0 border-surface-700">Product</th>
              <th class="bg-surface-800 text-surface-0 border-surface-700">Amount</th>
              <th class="bg-surface-800 text-surface-0 border-surface-700">Price</th>
              <th class="bg-surface-800 text-surface-0 border-surface-700 w-32">Actions</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-order>
            <tr class="hover:bg-surface-800 transition-colors border-surface-700">
              <td class="text-surface-200 border-surface-700">{{ order.createdOn | date: 'dd-MM-yyyy HH:mm' }}</td>
              <td class="text-surface-200 border-surface-700">{{ order.productName }}</td>
              <td class="text-surface-200 border-surface-700">{{ order.amount }}x</td>
              <td class="text-surface-200 border-surface-700 font-medium text-primary">{{ order.paid | currency: 'EUR' }}</td>
              <td class="text-surface-200 border-surface-700">
                <p-button
                  icon="pi pi-trash"
                  label="Delete"
                  severity="danger"
                  [outlined]="true"
                  size="small"
                  [loading]="deletingOrderId() === order.id"
                  (onClick)="onDeleteClick(order)"
                />
              </td>
            </tr>
          </ng-template>
          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="5" class="text-center p-8 text-surface-400">No orders found.</td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      animation: fadeIn 0.3s ease-out;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
    }
    ::ng-deep .p-datatable {
      .p-datatable-thead > tr > th {
        background: var(--p-surface-800);
        color: var(--p-surface-0);
        border-color: var(--p-surface-700);
        padding: 1rem;
      }
      .p-datatable-tbody > tr {
        background: transparent;
        color: var(--p-surface-200);
        border-color: var(--p-surface-700);
        &:hover {
          background: var(--p-surface-800) !important;
        }
        > td {
          border-color: var(--p-surface-700);
          padding: 1rem;
        }
      }
      .p-paginator {
        background: var(--p-surface-900);
        border-color: var(--p-surface-700);
        color: var(--p-surface-300);
        padding: 0.75rem;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class PersonalOrdersComponent implements OnInit {
  private orderService = inject(OrderService);
  private confirmationService = inject(ConfirmationService);
  private messageService = inject(MessageService);

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
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load orders' });
        }
      });
  }

  onDeleteClick(order: OrderDto) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete this order for ${order.productName}? This action cannot be undone.`,
      header: 'Confirm Deletion',
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
          this.messageService.add({ severity: 'success', summary: 'Confirmed', detail: 'Order deleted successfully' });
          this.refreshTable();
        },
        error: (err) => {
          console.error('Failed to delete order', err);
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete order' });
        }
      });
  }

  private refreshTable() {
    const table = this.dt();
    if (table) {
      table.onLazyLoad.emit(table.createLazyLoadMetadata());
    }
  }
}
