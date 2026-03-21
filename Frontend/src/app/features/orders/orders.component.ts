import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { CheckboxModule } from 'primeng/checkbox';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { OrderService } from '../../core/services/order.service';
import { ApplicationUserDto } from '../../core/models/application-user.model';
import { ProductDto } from '../../core/models/product.model';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ToastModule,
    CheckboxModule,
    InputNumberModule,
    ButtonModule
  ],
  providers: [MessageService],
  template: `
  <div class="bg-surface-0 dark:bg-surface-900 p-8 rounded-xl shadow-md text-surface-900 dark:text-surface-0">
    <div class="text-surface-900 dark:text-surface-0 h-full">
      <p-toast></p-toast>
      <div class="grid grid-cols-1 md:grid-cols-12 gap-8 w-full max-w-7xl mx-auto">
        <!-- Wat Column -->
        <div class="col-span-1 md:col-span-3">
          <h2 class="text-2xl font-bold mb-4 font-heading">Wat</h2>
          <div class="flex flex-col gap-3">
            @for (product of products(); track product.id) {
              <div 
                class="flex items-center p-3 border border-surface-200 dark:border-surface-700 rounded-lg hover:bg-surface-50 dark:hover:bg-surface-800 transition-colors"
                [class.ring-1]="selectedProducts().includes(product.id)"
                [class.ring-primary]="selectedProducts().includes(product.id)">
                <p-checkbox 
                  [value]="product.id" 
                  [ngModel]="selectedProducts()"
                  (ngModelChange)="selectedProducts.set($event)"
                  [inputId]="product.id">
                </p-checkbox>
                <label [for]="product.id" class="ml-3 flex flex-col w-full cursor-pointer">
                  <span class="font-medium font-body">{{ product.name }}</span>
                  <span class="text-sm text-surface-500 dark:text-surface-400 font-body">{{ product.price | currency:'EUR':'symbol':'1.2-2' }}</span>
                </label>
              </div>
            }
          </div>
        </div>

        <!-- Wie Column -->
        <div class="col-span-1 md:col-span-5">
          <h2 class="text-2xl font-bold mb-4 font-heading">Wie</h2>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            @for (user of users(); track user.id) {
              <div 
                class="flex items-center p-3 border border-surface-200 dark:border-surface-700 rounded-lg hover:bg-surface-50 dark:hover:bg-surface-800 transition-colors"
                [class.ring-1]="selectedUsers().includes(user.id)"
                [class.ring-primary]="selectedUsers().includes(user.id)">
                <p-checkbox 
                  [value]="user.id" 
                  [ngModel]="selectedUsers()"
                  (ngModelChange)="selectedUsers.set($event)"
                  [inputId]="user.id">
                </p-checkbox>
                <label [for]="user.id" class="ml-3 w-full font-medium font-body cursor-pointer">
                  {{ user.fullName || user.userName }}
                </label>
              </div>
            }
          </div>
        </div>

        <!-- Betalen Column -->
        <div class="col-span-1 md:col-span-4">
          <h2 class="text-2xl font-bold mb-4 font-heading">Betalen</h2>
          
          <div class="flex items-center mb-4">
            <p-checkbox [ngModel]="split()" (ngModelChange)="split.set($event)" [binary]="true" inputId="splitCheck"></p-checkbox>
            <label for="splitCheck" class="ml-2 font-medium font-body cursor-pointer">Rekening splitten?</label>
          </div>

          <div class="flex items-stretch border border-surface-200 dark:border-surface-700 rounded-lg overflow-hidden bg-surface-0 dark:bg-surface-900 border-opacity-50">
            <div class="flex items-center justify-center bg-surface-100 dark:bg-surface-800 px-4 font-medium font-body border-r border-surface-200 dark:border-surface-700">
              Aantal
            </div>
            <input 
              type="number" 
              [ngModel]="amount()"
              (ngModelChange)="amount.set($event)"
              class="w-full bg-transparent p-3 outline-none font-body text-surface-900 dark:text-surface-0 min-w-0" 
              min="1"
            />
            <button 
              pButton 
              label="Betalen" 
              class="p-button-danger rounded-none px-6 font-medium font-heading whitespace-nowrap"
              [disabled]="isSubmitting() || selectedProducts().length === 0 || selectedUsers().length === 0 || amount() < 1"
              [loading]="isSubmitting()"
              (click)="submitOrder()">
            </button>
          </div>
        </div>

      </div>
    </div>
  </div>
  `,
  styles: [`
    ::ng-deep .p-checkbox .p-checkbox-box {
      border-radius: 4px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class OrdersComponent implements OnInit {
  private orderService = inject(OrderService);
  private messageService = inject(MessageService);

  isLoading = signal(true);
  isSubmitting = signal(false);

  products = signal<ProductDto[]>([]);
  users = signal<ApplicationUserDto[]>([]);

  selectedProducts = signal<string[]>([]);
  selectedUsers = signal<string[]>([]);
  amount = signal<number>(1);
  split = signal<boolean>(false);

  ngOnInit() {
    this.orderService.initialize().subscribe({
      next: (data) => {
        this.products.set(data.products || []);
        this.users.set(data.users || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load order initial data' });
        this.isLoading.set(false);
        console.error(err);
      }
    });
  }

  submitOrder() {
    if (this.selectedProducts().length === 0 || this.selectedUsers().length === 0 || this.amount() < 1) {
      return;
    }

    this.isSubmitting.set(true);

    this.orderService.submit({
      products: this.selectedProducts(),
      users: this.selectedUsers(),
      amount: this.amount(),
      split: this.split()
    }).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Order geplaatst!' });
        this.isSubmitting.set(false);
        // Reset form
        this.selectedProducts.set([]);
        this.selectedUsers.set([]);
        this.amount.set(1);
        this.split.set(false);
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Bestelling mislukt' });
        this.isSubmitting.set(false);
        console.error(err);
      }
    });
  }
}

