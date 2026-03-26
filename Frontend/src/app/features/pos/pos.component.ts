import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { CheckboxModule } from 'primeng/checkbox';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { OrderService } from '../../core/services/order.service';
import { ApplicationUserDto } from '../../core/models/application-user.model';
import { ProductDto } from '../../core/models/product.model';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { AchievementPopupService } from '../../core/services/achievement-popup.service';

import { CardModule } from 'primeng/card';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
  selector: 'app-pos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CheckboxModule,
    InputNumberModule,
    ButtonModule,
    CardModule,
    ProgressSpinnerModule
  ],
  template: `
  <p-card>
    <div class="text-surface-900 dark:text-surface-0 h-full">
      @if (isLoading()) {
        <div class="flex justify-center items-center p-12">
          <p-progressSpinner ariaLabel="loading"></p-progressSpinner>
        </div>
      } @else {
        <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 w-full max-w-7xl mx-auto">
          <!-- Wat Column -->
          <div class="col-span-1 lg:col-span-3">
            <h2 class="text-2xl font-bold mb-4 font-heading">{{ ls.t().what }}</h2>
            <div class="grid grid-cols-2 lg:grid-cols-1 gap-3">
              @for (product of products(); track product.id) {
                <label 
                  [for]="product.id"
                  class="flex items-center p-3 border border-surface-200 dark:border-surface-700 rounded-lg hover:bg-surface-50 dark:hover:bg-surface-800 transition-colors cursor-pointer"
                  [class.ring-1]="selectedProducts().includes(product.id)"
                  [class.ring-primary]="selectedProducts().includes(product.id)">
                  <p-checkbox 
                    [value]="product.id" 
                    [ngModel]="selectedProducts()"
                    (ngModelChange)="selectedProducts.set($event)"
                    [inputId]="product.id">
                  </p-checkbox>
                  <div class="ml-3 flex flex-col w-full">
                    <span class="font-medium font-body">{{ product.name }}</span>
                    <span class="text-sm text-surface-500 dark:text-surface-400 font-body">{{ product.price | currency:'EUR':'symbol':'1.2-2' }}</span>
                  </div>
                </label>
              }
            </div>
          </div>

          <!-- Wie Column -->
          <div class="col-span-1 lg:col-span-5">
            <h2 class="text-2xl font-bold mb-4 font-heading">{{ ls.t().who }}</h2>
            <div class="grid grid-cols-2 gap-3">
              @for (user of users(); track user.id) {
                <label 
                  [for]="user.id"
                  class="flex items-center p-3 border border-surface-200 dark:border-surface-700 rounded-lg hover:bg-surface-50 dark:hover:bg-surface-800 transition-colors cursor-pointer"
                  [class.ring-1]="selectedUsers().includes(user.id)"
                  [class.ring-primary]="selectedUsers().includes(user.id)">
                  <p-checkbox 
                    [value]="user.id" 
                    [ngModel]="selectedUsers()"
                    (ngModelChange)="selectedUsers.set($event)"
                    [inputId]="user.id">
                  </p-checkbox>
                  <div class="ml-3 w-full font-medium font-body">
                    {{ user.fullName || user.userName }}
                  </div>
                </label>
              }
            </div>
          </div>

          <!-- Betalen Column -->
          <div class="col-span-1 lg:col-span-4">
            <h2 class="text-2xl font-bold mb-4 font-heading">{{ ls.t().pay }}</h2>
            
            <label for="splitCheck" class="flex items-center mb-4 cursor-pointer w-fit">
              <p-checkbox [ngModel]="split()" (ngModelChange)="split.set($event)" [binary]="true" inputId="splitCheck"></p-checkbox>
              <div class="ml-2 font-medium font-body">{{ ls.t().split_bill }}</div>
            </label>

            <div class="flex flex-col gap-4">
              <div class="flex items-stretch border border-surface-200 dark:border-surface-700 rounded-lg overflow-hidden bg-surface-0 dark:bg-surface-900 border-opacity-50">
                <div class="flex items-center justify-center bg-surface-100 dark:bg-surface-800 px-4 font-medium font-body border-r border-surface-200 dark:border-surface-700">
                  {{ ls.t().amount }}
                </div>
                <input 
                  type="number" 
                  [ngModel]="amount()"
                  (ngModelChange)="amount.set($event)"
                  class="w-full bg-transparent p-3 outline-none font-body text-surface-900 dark:text-surface-0 min-w-0" 
                  min="1"
                />
              </div>
              <p-button 
                [label]="ls.t().order"
                icon="pi pi-credit-card" 
                styleClass="p-button-primary w-full justify-center p-3 text-xl rounded-lg"
                [style]="{'width': '100%'}"
                [disabled]="isSubmitting() || selectedProducts().length === 0 || selectedUsers().length === 0 || amount() < 1 || isGuest()"
                [loading]="isSubmitting()"
                (onClick)="submitOrder()">
              </p-button>
            </div>
          </div>

        </div>
      }
    </div>
  </p-card>
  `,
  styles: [`
    ::ng-deep .p-checkbox .p-checkbox-box {
      border-radius: 4px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class PosComponent implements OnInit {
  private orderService = inject(OrderService);
  private messageService = inject(MessageService);
  private authService = inject(AuthService);
  private popupService = inject(AchievementPopupService);
  ls = inject(LanguageService);

  isLoading = signal(true);
  isSubmitting = signal(false);
  isGuest = computed(() => this.authService.currentUser()?.roles?.includes('Guest') ?? false);

  products = signal<ProductDto[]>([]);
  users = signal<ApplicationUserDto[]>([]);

  selectedProducts = signal<string[]>([]);
  selectedUsers = signal<string[]>([]);
  amount = signal<number>(1);
  split = signal<boolean>(false);

  ngOnInit() {
    this.orderService.initialize().subscribe({
      next: (data) => {
        const productData = data.products || [];
        const userData = data.users || [];
        this.products.set(productData);
        this.users.set(userData);
        
        const currentUser = this.authService.currentUser();
        const currentUserId = currentUser?.id;

        if (currentUserId && userData.some(u => u.id === currentUserId) && !currentUser?.roles?.includes('Kiosk')) {
            this.selectedUsers.set([currentUserId]);
        }

        this.isLoading.set(false);
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
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
        this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().order_placed });
        this.authService.refreshCurrentUser();
        this.isSubmitting.set(false);

        // Check for newly acquired achievements after placing order
        this.popupService.checkUnseenAchievements();

        const currentUser = this.authService.currentUser();
        const currentUserId = currentUser?.id;
        
        // Reset form
        this.selectedProducts.set([]);
        if (currentUserId && this.users().some(u => u.id === currentUserId) && !currentUser?.roles?.includes('Kiosk')) {
          this.selectedUsers.set([currentUserId]);
        } else {
          this.selectedUsers.set([]);
        }
        
        this.amount.set(1);
        this.split.set(false);
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().order_failed });
        this.isSubmitting.set(false);
        console.error(err);
      }
    });
  }
}

