import { Component, ChangeDetectionStrategy, inject, signal, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { FormsModule } from '@angular/forms';
import { AvatarModule } from 'primeng/avatar';
import { LeaderboardService } from '../../core/services/leaderboard.service';
import { ProductService } from '../../core/services/product.service';
import { LeaderboardEntryDto } from '../../core/models/leaderboard.model';
import { ProductDto } from '../../core/models/product.model';
import { LanguageService } from '../../core/services/language.service';
import { forkJoin, finalize } from 'rxjs';

@Component({
  selector: 'app-leaderboard',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    SelectModule,
    ButtonModule,
    CardModule,
    FormsModule,
    AvatarModule
  ],
  template: `
    <div class="flex justify-between items-center mb-6 px-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().leaderboard }}</h1>
      <p-button icon="pi pi-refresh" [rounded]="true" (onClick)="refreshAll()" [loading]="loading()" [ariaLabel]="ls.t().refresh"></p-button>
    </div>

    <div class="flex flex-col gap-8 px-4 pb-8 max-w-5xl mx-auto">
      <div class="flex flex-col gap-2">
        <label for="product-select" class="text-surface-600 dark:text-surface-400 font-medium">{{ ls.t().select_product }}</label>
        <p-select
          id="product-select"
          [options]="products()"
          [(ngModel)]="selectedProduct"
          optionLabel="name"
          placeholder="Selecteer een product"
          styleClass="w-full md:w-80"
          (onChange)="onProductChange()"
        ></p-select>
      </div>

      <!-- Last 12 Hours -->
      <p-card>
        <h2 class="text-xl font-bold mb-4 text-surface-900 dark:text-surface-0">{{ ls.t().last_12_hours }}</h2>
        @if (leaderboard12h().length === 0 && !loading()) {
            <p class="text-surface-600 dark:text-surface-400 italic mb-4">{{ ls.t().no_purchases_period }}</p>
        } @else {
            <p-table stripedRows [value]="leaderboard12h()" [loading]="loading()" responsiveLayout="scroll" styleClass="p-datatable-sm">
                <ng-template pTemplate="header">
                    <tr>
                        <th class="w-20">{{ ls.t().rank }}</th>
                        <th>{{ ls.t().name }}</th>
                        <th class="w-32">{{ ls.t().amount }}</th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-entry>
                    <tr>
                        <td class="font-bold">#{{ entry.position }}</td>
                        <td>
                            <div class="flex items-center gap-3">
                                <p-avatar 
                                    [image]="entry.profilePictureUrl" 
                                    [label]="!entry.profilePictureUrl ? entry.fullName.substring(0,1) : undefined" 
                                    shape="circle">
                                </p-avatar>
                                <span>{{ entry.fullName }}</span>
                            </div>
                        </td>
                        <td>{{ entry.amount }}</td>
                    </tr>
                </ng-template>
            </p-table>
        }
      </p-card>

      <!-- Monthly -->
      <p-card>
        <h2 class="text-xl font-bold mb-4 text-surface-900 dark:text-surface-0">{{ ls.t().monthly }}</h2>
        <p-table stripedRows [value]="leaderboardMonthly()" [loading]="loading()" responsiveLayout="scroll" styleClass="p-datatable-sm">
            <ng-template pTemplate="header">
                <tr>
                    <th class="w-20">{{ ls.t().rank }}</th>
                    <th>{{ ls.t().name }}</th>
                    <th class="w-32">{{ ls.t().amount }}</th>
                </tr>
            </ng-template>
            <ng-template pTemplate="body" let-entry>
                <tr>
                    <td class="font-bold">#{{ entry.position }}</td>
                    <td>
                        <div class="flex items-center gap-3">
                            <p-avatar 
                                [image]="entry.profilePictureUrl" 
                                [label]="!entry.profilePictureUrl ? entry.fullName.substring(0,1) : undefined" 
                                shape="circle">
                            </p-avatar>
                            <span>{{ entry.fullName }}</span>
                        </div>
                    </td>
                    <td>{{ entry.amount }}</td>
                </tr>
            </ng-template>
            <ng-template pTemplate="emptymessage">
                <tr>
                    <td colspan="3" class="text-center p-4 text-surface-500 italic">{{ ls.t().no_purchases_period }}</td>
                </tr>
            </ng-template>
        </p-table>
      </p-card>

      <!-- Total -->
      <p-card>
        <h2 class="text-xl font-bold mb-4 text-surface-900 dark:text-surface-0">{{ ls.t().total }}</h2>
        <p-table stripedRows [value]="leaderboardTotal()" [loading]="loading()" responsiveLayout="scroll" styleClass="p-datatable-sm">
            <ng-template pTemplate="header">
                <tr>
                    <th class="w-20">{{ ls.t().rank }}</th>
                    <th>{{ ls.t().name }}</th>
                    <th class="w-32">{{ ls.t().amount }}</th>
                </tr>
            </ng-template>
            <ng-template pTemplate="body" let-entry>
                <tr>
                    <td class="font-bold">#{{ entry.position }}</td>
                    <td>
                        <div class="flex items-center gap-3">
                            <p-avatar 
                                [image]="entry.profilePictureUrl" 
                                [label]="!entry.profilePictureUrl ? entry.fullName.substring(0,1) : undefined" 
                                shape="circle">
                            </p-avatar>
                            <span>{{ entry.fullName }}</span>
                        </div>
                    </td>
                    <td>{{ entry.amount }}</td>
                </tr>
            </ng-template>
            <ng-template pTemplate="emptymessage">
                <tr>
                    <td colspan="3" class="text-center p-4 text-surface-500 italic">{{ ls.t().no_purchases_period }}</td>
                </tr>
            </ng-template>
        </p-table>
      </p-card>
    </div>
  `,
  styles: `
    :host {
      display: block;
      padding-top: 1rem;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class LeaderboardComponent {
  private readonly leaderboardService = inject(LeaderboardService);
  private readonly productService = inject(ProductService);
  ls = inject(LanguageService);

  products = signal<ProductDto[]>([]);
  selectedProduct = signal<ProductDto | null>(null);
  
  leaderboard12h = signal<LeaderboardEntryDto[]>([]);
  leaderboardMonthly = signal<LeaderboardEntryDto[]>([]);
  leaderboardTotal = signal<LeaderboardEntryDto[]>([]);
  
  loading = signal<boolean>(false);

  constructor() {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.productService.getProducts(0, 1000, true).subscribe({
      next: (data) => {
        const productList = data as ProductDto[];
        this.products.set(productList);
        if (productList.length > 0) {
          this.selectedProduct.set(productList[0]);
          this.refreshAll();
        } else {
          this.loading.set(false);
        }
      },
      error: () => this.loading.set(false)
    });
  }

  onProductChange(): void {
    this.refreshAll();
  }

  refreshAll(): void {
    const product = this.selectedProduct();
    if (!product) return;

    this.loading.set(true);
    forkJoin({
      total: this.leaderboardService.getLeaderboard(product.id),
      monthly: this.leaderboardService.getMonthlyLeaderboard(product.id),
      last12h: this.leaderboardService.get12HourLeaderboard(product.id)
    }).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe({
      next: (results) => {
        this.leaderboardTotal.set(results.total);
        this.leaderboardMonthly.set(results.monthly);
        this.leaderboard12h.set(results.last12h);
      }
    });
  }
}
