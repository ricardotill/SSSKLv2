import { Component, ChangeDetectionStrategy, signal, computed, AfterViewInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ApplicationUserDto } from '../../core/models/application-user.model';
import { ProductDto } from '../../core/models/product.model';

// ---------------------------------------------------------------------------
// Fake data – enough users to force scrolling on mobile
// ---------------------------------------------------------------------------
const FAKE_PRODUCTS: ProductDto[] = [
  { id: 'p1', name: 'Biertje', description: null, price: 1.50, stock: 99, enableLeaderboard: true },
  { id: 'p2', name: 'Fris', description: null, price: 1.00, stock: 99, enableLeaderboard: false },
  { id: 'p3', name: 'Wijn', description: null, price: 2.00, stock: 99, enableLeaderboard: true },
  { id: 'p4', name: 'Koffie', description: null, price: 1.25, stock: 99, enableLeaderboard: false },
  { id: 'p5', name: 'Thee', description: null, price: 1.00, stock: 99, enableLeaderboard: false },
];

const FAKE_USERS: ApplicationUserDto[] = [
  { id: 'u01', userName: 'alice',   fullName: 'Alice de Vries',    saldo: 10, roles: ['User'] },
  { id: 'u02', userName: 'bob',     fullName: 'Bob Janssen',        saldo: 5,  roles: ['User'] },
  { id: 'u03', userName: 'charlie', fullName: 'Charlie Bakker',     saldo: 8,  roles: ['User'] },
  { id: 'u04', userName: 'diana',   fullName: 'Diana van den Berg', saldo: 12, roles: ['User'] },
  { id: 'u05', userName: 'eve',     fullName: 'Eve Smits',          saldo: 3,  roles: ['User'] },
  { id: 'u06', userName: 'frank',   fullName: 'Frank Mulder',       saldo: 20, roles: ['User'] },
  { id: 'u07', userName: 'grace',   fullName: 'Grace Visser',       saldo: 7,  roles: ['User'] },
  { id: 'u08', userName: 'hank',    fullName: 'Hank de Groot',      saldo: 9,  roles: ['User'] },
  { id: 'u09', userName: 'iris',    fullName: 'Iris Meijer',        saldo: 15, roles: ['User'] },
  { id: 'u10', userName: 'jan',     fullName: 'Jan Peters',         saldo: 4,  roles: ['User'] },
  { id: 'u11', userName: 'karen',   fullName: 'Karen van Dijk',     saldo: 11, roles: ['User'] },
  { id: 'u12', userName: 'lars',    fullName: 'Lars Bos',           saldo: 6,  roles: ['User'] },
  { id: 'u13', userName: 'maria',   fullName: 'Maria Hendriks',     saldo: 18, roles: ['User'] },
  { id: 'u14', userName: 'niels',   fullName: 'Niels Kuiper',       saldo: 2,  roles: ['User'] },
  { id: 'u15', userName: 'olivia',  fullName: 'Olivia Kok',         saldo: 13, roles: ['User'] },
  { id: 'u16', userName: 'piet',    fullName: 'Piet van Leeuwen',   saldo: 16, roles: ['User'] },
  { id: 'u17', userName: 'quirine', fullName: 'Quirine Brouwer',    saldo: 14, roles: ['User'] },
  { id: 'u18', userName: 'rob',     fullName: 'Rob de Boer',        saldo: 19, roles: ['User'] },
  { id: 'u19', userName: 'sara',    fullName: 'Sara Vos',           saldo: 8,  roles: ['User'] },
  { id: 'u20', userName: 'tom',     fullName: 'Tom Laan',           saldo: 1,  roles: ['User'] },
];

@Component({
  selector: 'app-pos-demo',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    CheckboxModule,
    ButtonModule,
    CardModule,
    TagModule,
  ],
  template: `
  <!-- Dev-mode banner -->
  <div class="demo-banner">
    <span class="pi pi-flask mr-2"></span>
    POS Demo — fake data only, no orders are placed &nbsp;
    <p-tag value="DEV" severity="warn" />
  </div>

  <p-card>
    <div class="text-surface-900 dark:text-surface-0 h-full">
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 w-full max-w-7xl mx-auto">

        <!-- Wat Column -->
        <div class="col-span-1 lg:col-span-3">
          <h2 class="text-2xl font-bold mb-4 font-heading">Wat</h2>
          <div class="grid grid-cols-2 lg:grid-cols-1 gap-3">
            @for (product of products; track product.id) {
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
          <h2 class="text-2xl font-bold mb-4 font-heading">Wie <span class="text-sm font-normal text-surface-500">({{ users.length }} users – scroll down to see floating bar)</span></h2>
          <div class="grid grid-cols-2 gap-3">
            @for (user of users; track user.id) {
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
          <h2 class="text-2xl font-bold mb-4 font-heading">Betalen</h2>
          <!-- Sentinel watched by IntersectionObserver -->
          <div #payColumnSentinel></div>
          <ng-container *ngTemplateOutlet="payControls"></ng-container>
        </div>

      </div>
    </div>
  </p-card>

  <!-- Shared Pay controls -->
  <ng-template #payControls>
    <label for="splitCheckDemo" class="flex items-center mb-4 cursor-pointer w-fit">
      <p-checkbox [ngModel]="split()" (ngModelChange)="split.set($event)" [binary]="true" inputId="splitCheckDemo"></p-checkbox>
      <div class="ml-2 font-medium font-body">Rekening splitsen</div>
    </label>

    <div class="flex flex-col gap-4">
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
      </div>
      <p-button
        label="Bestellen"
        icon="pi pi-credit-card"
        styleClass="p-button-primary w-full justify-center p-3 text-xl rounded-lg"
        [style]="{'width': '100%'}"
        [disabled]="selectedProducts().length === 0 || selectedUsers().length === 0 || amount() < 1"
        (onClick)="fakePlaceOrder()">
      </p-button>
    </div>

    @if (lastOrder()) {
      <div class="mt-4 p-3 rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-sm text-green-800 dark:text-green-300 font-body">
        <span class="pi pi-check-circle mr-2"></span>{{ lastOrder() }}
      </div>
    }
  </ng-template>

  <!-- Floating bottom bar – mirrors real POS behaviour -->
  @if (showFloatingBar()) {
    <div class="pos-floating-bar">
      <ng-container *ngTemplateOutlet="payControls"></ng-container>
    </div>
  }
  `,
  styles: [`
    .demo-banner {
      display: flex;
      align-items: center;
      padding: 0.5rem 1rem;
      margin-bottom: 1rem;
      background: color-mix(in srgb, var(--p-yellow-100, #fef9c3) 60%, transparent);
      border: 1px solid var(--p-yellow-300, #fde047);
      border-radius: 0.5rem;
      font-size: 0.875rem;
      font-weight: 500;
      color: var(--p-yellow-900, #713f12);
    }

    ::ng-deep .p-checkbox .p-checkbox-box {
      border-radius: 4px;
    }

    .pos-floating-bar {
      position: fixed;
      bottom: 0;
      left: 0;
      right: 0;
      z-index: 1000;
      padding: 1rem 1.25rem;
      background: var(--p-surface-card);
      border-top: 1px solid var(--p-surface-border);
      box-shadow: 0 -4px 24px rgba(0, 0, 0, 0.15);
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      animation: posSlideUp 0.25s ease-out;
    }

    :host-context(.dark) .pos-floating-bar {
      background: var(--p-surface-900);
      border-top-color: var(--p-surface-700);
      box-shadow: 0 -4px 24px rgba(0, 0, 0, 0.4);
    }

    @keyframes posSlideUp {
      from { transform: translateY(100%); opacity: 0; }
      to   { transform: translateY(0);    opacity: 1; }
    }

    @media (min-width: 1024px) {
      .pos-floating-bar { display: none !important; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PosDemoComponent implements AfterViewInit, OnDestroy {
  @ViewChild('payColumnSentinel') private payColumnSentinel!: ElementRef<HTMLElement>;
  private intersectionObserver: IntersectionObserver | null = null;

  readonly products = FAKE_PRODUCTS;
  readonly users = FAKE_USERS;

  selectedProducts = signal<string[]>([]);
  selectedUsers = signal<string[]>([]);
  amount = signal<number>(1);
  split = signal<boolean>(false);
  showFloatingBar = signal(false);
  lastOrder = signal<string | null>(null);

  ngAfterViewInit() {
    this.intersectionObserver = new IntersectionObserver(
      ([entry]) => this.showFloatingBar.set(!entry.isIntersecting),
      { threshold: 0 }
    );
    if (this.payColumnSentinel) {
      this.intersectionObserver.observe(this.payColumnSentinel.nativeElement);
    }
  }

  ngOnDestroy() {
    this.intersectionObserver?.disconnect();
    this.intersectionObserver = null;
  }

  fakePlaceOrder() {
    const productNames = this.selectedProducts()
      .map(id => this.products.find(p => p.id === id)?.name ?? id)
      .join(', ');
    const userNames = this.selectedUsers()
      .map(id => this.users.find(u => u.id === id)?.fullName ?? id)
      .join(', ');

    this.lastOrder.set(
      `[Demo] ${productNames} × ${this.amount()} voor ${userNames}${this.split() ? ' (gesplitst)' : ''}`
    );

    // Reset
    this.selectedProducts.set([]);
    this.amount.set(1);
    this.split.set(false);
  }
}
