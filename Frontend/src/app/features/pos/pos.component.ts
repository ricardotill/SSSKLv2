import { Component, ChangeDetectionStrategy, DOCUMENT, ElementRef, inject, signal, computed, effect, viewChild, OnInit, LOCALE_ID } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { MessageService } from 'primeng/api';
import { SkeletonModule } from 'primeng/skeleton';
import { OrderService } from '../orders/services/order.service';
import { ApplicationUserDto } from '../../core/models/application-user.model';
import { ProductDto } from '../../core/models/product.model';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { AchievementPopupService } from '../achievements/services/achievement-popup.service';
import { ResolveApiUrlPipe } from '../../shared/pipes/resolve-api-url.pipe';

/** Show the people search field once the list no longer fits comfortably on a phone screen. */
const USER_SEARCH_THRESHOLD = 6;
const MIN_AMOUNT = 1;
const MAX_AMOUNT = 99;

interface PosUser extends ApplicationUserDto {
  displayName: string;
  initials: string;
}

function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return '?';
  }
  const first = parts[0][0];
  const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
  return (first + last).toUpperCase();
}

@Component({
  selector: 'app-pos',
  imports: [CurrencyPipe, SkeletonModule, ResolveApiUrlPipe],
  template: `
  <div class="pos-page text-surface-900 dark:text-surface-0">
    @if (isLoading()) {
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-8" [attr.aria-label]="ls.t().loading" aria-busy="true">
        <div class="lg:col-span-3 grid grid-cols-3 lg:grid-cols-1 gap-2.5">
          @for (i of skeletonTiles; track i) { <p-skeleton height="4.5rem" borderRadius="1rem" /> }
        </div>
        <div class="lg:col-span-5 grid grid-cols-2 gap-2.5">
          @for (i of skeletonTiles; track i) { <p-skeleton height="3.5rem" borderRadius="1rem" /> }
        </div>
      </div>
    } @else {
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-7 lg:gap-8 w-full max-w-7xl mx-auto">
        <!-- Wat -->
        <section class="lg:col-span-3" aria-labelledby="pos-what">
          <div class="section-head">
            <h2 id="pos-what" class="section-title font-heading">{{ ls.t().what }}</h2>
            @if (selectedProducts().length > 0) {
              <span class="section-meta">{{ ls.translate('selected_count', { count: selectedProducts().length }) }}</span>
            }
          </div>
          <div class="grid grid-cols-3 sm:grid-cols-4 lg:grid-cols-1 gap-2.5">
            @for (product of products(); track product.id) {
              <label class="tile product-tile">
                <input type="checkbox" class="sr-only"
                  [checked]="selectedProductIds().has(product.id)"
                  (change)="toggleProduct(product.id)" />
                <span class="font-semibold leading-snug break-words hyphens-auto" lang="nl">{{ product.name }}</span>
                <span class="text-sm tabular-nums text-surface-600 dark:text-surface-300">{{ product.price | currency:'EUR':'symbol':'1.2-2' }}</span>
                <span class="check-badge" aria-hidden="true"><i class="pi pi-check"></i></span>
              </label>
            }
          </div>
        </section>

        <!-- Wie -->
        <section class="lg:col-span-5" aria-labelledby="pos-who">
          <div class="section-head">
            <h2 id="pos-who" class="section-title font-heading">{{ ls.t().who }}</h2>
            @if (selectedUsers().length > 0) {
              <span class="section-meta">{{ ls.translate('people_count', { count: selectedUsers().length }) }}</span>
            }
          </div>
          @if (showUserSearch()) {
            <div class="relative mb-3">
              <i class="pi pi-search absolute left-4 top-1/2 -translate-y-1/2 text-surface-500 dark:text-surface-400" aria-hidden="true"></i>
              <input type="search" enterkeyhint="search" autocomplete="off"
                class="search-input"
                [placeholder]="ls.t().search_people"
                [attr.aria-label]="ls.t().search_people"
                [value]="userQuery()"
                (input)="userQuery.set($any($event.target).value)" />
            </div>
          }
          <div class="grid grid-cols-2 gap-2.5">
            @for (user of visibleUsers(); track user.id) {
              <label class="tile user-tile">
                <input type="checkbox" class="sr-only"
                  [checked]="selectedUserIds().has(user.id)"
                  (change)="toggleUser(user.id)" />
                <span class="avatar" aria-hidden="true">
                  @if (user.profilePictureUrl) {
                    <img [src]="user.profilePictureUrl | resolveApiUrl" alt="" loading="lazy" />
                  } @else {
                    {{ user.initials }}
                  }
                  <span class="avatar-check"><i class="pi pi-check"></i></span>
                </span>
                <span class="text-sm font-medium leading-snug line-clamp-2 min-w-0">{{ user.displayName }}</span>
              </label>
            } @empty {
              <p class="col-span-2 py-6 text-center text-surface-600 dark:text-surface-300">{{ ls.t().no_people_found }}</p>
            }
          </div>
        </section>

        <!--
          Betalen: one element for both layouts. On phones it is a bottom sheet that is always
          within thumb reach; from lg it is a sticky third column.
        -->
        <section #paySection class="pos-pay lg:col-span-4" aria-labelledby="pos-pay">
          <h2 id="pos-pay" class="sr-only lg:not-sr-only section-title font-heading lg:mb-4">{{ ls.t().pay }}</h2>
          <p class="sr-only" aria-live="polite">{{ summaryText() }}</p>

          <div class="flex items-center gap-2.5 mb-3">
            <div class="stepper" role="group" [attr.aria-label]="ls.t().amount">
              <button type="button" class="stepper-btn"
                [attr.aria-label]="ls.t().decrease_amount"
                [disabled]="amount() <= minAmount"
                (click)="changeAmount(-1)"><i class="pi pi-minus" aria-hidden="true"></i></button>
              <input type="number" inputmode="numeric" pattern="[0-9]*"
                class="stepper-input"
                [attr.aria-label]="ls.t().amount"
                [min]="minAmount" [max]="maxAmount" step="1"
                [value]="amount()"
                (change)="setAmount($any($event.target).value); $any($event.target).value = amount()" />
              <button type="button" class="stepper-btn"
                [attr.aria-label]="ls.t().increase_amount"
                [disabled]="amount() >= maxAmount"
                (click)="changeAmount(1)"><i class="pi pi-plus" aria-hidden="true"></i></button>
            </div>

            @if (canSplit()) {
              <button type="button" class="split-chip"
                [attr.aria-pressed]="split()"
                (click)="split.set(!split())">
                <i class="pi" [class.pi-check]="split()" [class.pi-users]="!split()" aria-hidden="true"></i>
                {{ ls.t().split_short }}
              </button>
            }
          </div>

          <button type="button" class="order-btn"
            [disabled]="!canSubmit()"
            [attr.aria-busy]="isSubmitting()"
            (click)="submitOrder()">
            <span class="flex items-center gap-2.5 text-lg">
              <i class="pi" [class.pi-spin]="isSubmitting()" [class.pi-spinner]="isSubmitting()" [class.pi-shopping-cart]="!isSubmitting()" aria-hidden="true"></i>
              {{ ls.t().order }}
            </span>
            @if (summary(); as s) {
              <span class="flex flex-col items-end leading-tight">
                <span class="text-lg tabular-nums">{{ s.total | currency:'EUR':'symbol':'1.2-2' }}</span>
                @if (selectedUsers().length > 1) {
                  <span class="text-xs font-medium opacity-80 tabular-nums">{{ s.perUser | currency:'EUR':'symbol':'1.2-2' }} {{ ls.t().per_person }}</span>
                }
              </span>
            } @else {
              <span class="text-sm font-medium opacity-80">{{ ls.t().pos_select_hint }}</span>
            }
          </button>

          @if (isGuest()) {
            <p class="mt-2 text-sm text-center text-surface-600 dark:text-surface-300">{{ ls.t().guest_cannot_order }}</p>
          }
        </section>
      </div>
    }
  </div>
  `,
  styles: [`
    :host {
      display: block;
      --pos-bg: var(--p-surface-0);
      --pos-line: var(--p-surface-200);
      --pos-muted-bg: var(--p-surface-100);
      --pos-muted-fg: var(--p-surface-700);
      --pos-btn-bg: var(--p-surface-0);
      --pos-accent: var(--p-primary-500);
      --pos-accent-text: var(--p-primary-700);
      --pos-selected-bg: var(--p-primary-50);
      --pos-chip-on-bg: var(--p-primary-100);
      --pos-chip-on-fg: var(--p-primary-800);
      --pos-cta-bg: var(--p-primary-700); /* keeps white text above WCAG AA */
      --pos-cta-hover: var(--p-primary-800);
      --pos-cta-fg: #fff;
      --pos-sheet-bg: color-mix(in srgb, var(--p-surface-0) 88%, transparent);
      --pos-pay-bg: var(--p-surface-50);
    }
    :host-context(.dark) {
      --pos-bg: var(--p-surface-900);
      --pos-line: var(--p-surface-800);
      --pos-muted-bg: var(--p-surface-800);
      --pos-muted-fg: var(--p-surface-200);
      --pos-btn-bg: var(--p-surface-700);
      --pos-accent: var(--p-primary-400);
      --pos-accent-text: var(--p-primary-300);
      --pos-selected-bg: color-mix(in srgb, var(--p-primary-500) 14%, var(--p-surface-900));
      --pos-chip-on-bg: color-mix(in srgb, var(--p-primary-500) 20%, var(--p-surface-900));
      --pos-chip-on-fg: var(--p-primary-200);
      --pos-cta-bg: var(--p-primary-400);
      --pos-cta-hover: var(--p-primary-300);
      --pos-cta-fg: var(--p-surface-950);
      --pos-sheet-bg: color-mix(in srgb, var(--p-surface-900) 85%, transparent);
      --pos-pay-bg: var(--p-surface-800);
    }

    .pos-page { padding-bottom: calc(10rem + env(safe-area-inset-bottom)); }

    .section-head { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: .75rem; }
    .section-title { font-size: 1.25rem; font-weight: 700; letter-spacing: -.01em; }
    .section-meta { font-size: .875rem; font-weight: 500; color: var(--pos-accent-text); }

    /* Selectable tiles: a visually hidden native checkbox inside a label */
    .tile {
      position: relative; cursor: pointer; user-select: none; -webkit-tap-highlight-color: transparent;
      border-radius: 1rem; background: var(--pos-bg); box-shadow: 0 0 0 1px var(--pos-line);
      transition: box-shadow .15s, background-color .15s, transform .1s;
    }
    .tile:active { transform: scale(.97); }
    .tile:has(:checked) { background: var(--pos-selected-bg); box-shadow: 0 0 0 2px var(--pos-accent); }
    .tile:has(:focus-visible) { outline: 2px solid var(--pos-accent); outline-offset: 3px; }

    .product-tile {
      display: flex; flex-direction: column; justify-content: space-between; gap: .25rem;
      min-height: 4.5rem; padding: .75rem .875rem;
    }
    .product-tile:has(:checked) > span:first-of-type { padding-right: 1rem; }
    .user-tile { display: flex; align-items: center; gap: .625rem; min-height: 3.5rem; padding: .5rem .75rem .5rem .5rem; }

    .check-badge, .avatar-check {
      display: grid; place-items: center; border-radius: 9999px;
      background: var(--p-primary-500); color: #fff;
      opacity: 0; transform: scale(.5); transition: opacity .15s, transform .15s;
    }
    .tile:has(:checked) :is(.check-badge, .avatar-check) { opacity: 1; transform: none; }
    /* PrimeUI gives every .pi a fixed size, so size the icons themselves */
    .check-badge { position: absolute; top: .5rem; right: .5rem; width: 1.375rem; height: 1.375rem; }
    .check-badge .pi { font-size: .625rem; font-weight: 700; }
    .avatar-check { position: absolute; inset: 0; }
    .avatar-check .pi { font-size: .875rem; }

    .avatar {
      position: relative; flex: none; display: grid; place-items: center;
      width: 2.5rem; height: 2.5rem; border-radius: 9999px; overflow: hidden;
      font-size: .8125rem; font-weight: 700; letter-spacing: .02em;
      background: var(--pos-muted-bg); color: var(--pos-muted-fg);
    }
    .avatar img { width: 100%; height: 100%; object-fit: cover; }

    .search-input {
      width: 100%; min-height: 3rem; padding: 0 1rem 0 2.75rem; border: none; border-radius: 9999px;
      background: var(--pos-bg); color: inherit; box-shadow: 0 0 0 1px var(--pos-line); outline: none;
    }
    .search-input:focus-visible { box-shadow: 0 0 0 2px var(--pos-accent); }
    .search-input::placeholder { color: var(--p-surface-500); }

    /* Amount stepper: one pill */
    .stepper { display: inline-flex; align-items: center; padding: .25rem; border-radius: 9999px; background: var(--pos-muted-bg); }
    .stepper-btn {
      display: grid; place-items: center; width: 2.75rem; height: 2.75rem; border-radius: 9999px;
      background: var(--pos-btn-bg); color: inherit; box-shadow: 0 1px 2px rgba(0, 0, 0, .08);
      transition: transform .1s, opacity .15s;
    }
    .stepper-btn:disabled { opacity: .4; box-shadow: none; }
    .stepper-input {
      width: 2.75rem; border: none; background: transparent; color: inherit; text-align: center;
      font-size: 1.125rem; font-weight: 700; font-variant-numeric: tabular-nums; -moz-appearance: textfield;
    }
    .stepper-input::-webkit-inner-spin-button, .stepper-input::-webkit-outer-spin-button { -webkit-appearance: none; margin: 0; }

    .split-chip {
      display: inline-flex; align-items: center; gap: .5rem; height: 3.25rem; padding: 0 1.125rem;
      border-radius: 9999px; font-weight: 600; background: var(--pos-muted-bg); color: inherit;
      transition: background-color .15s, color .15s;
    }
    .split-chip[aria-pressed='true'] {
      background: var(--pos-chip-on-bg); color: var(--pos-chip-on-fg); box-shadow: inset 0 0 0 2px var(--pos-accent);
    }

    .order-btn {
      display: flex; align-items: center; justify-content: space-between; gap: 1rem;
      width: 100%; min-height: 3.5rem; padding: .5rem 1.25rem; border-radius: 1.125rem; font-weight: 700;
      background: var(--pos-cta-bg); color: var(--pos-cta-fg);
      box-shadow: 0 6px 20px -6px color-mix(in srgb, var(--p-primary-600) 60%, transparent);
      transition: transform .1s, background-color .15s, opacity .15s;
    }
    .order-btn:hover:not(:disabled) { background: var(--pos-cta-hover); }
    .order-btn:disabled { cursor: not-allowed; box-shadow: none; opacity: .55; }

    .stepper-btn:active:not(:disabled), .order-btn:active:not(:disabled) { transform: scale(.95); }
    :is(.stepper-btn, .split-chip, .order-btn):focus-visible { outline: 2px solid var(--pos-accent); outline-offset: 2px; }
    .stepper-input:focus-visible { outline: 2px solid var(--pos-accent); border-radius: .5rem; }

    /* Phones and tablets: the pay section is a frosted bottom sheet */
    @media (max-width: 1023.98px) {
      .pos-pay {
        position: fixed; inset-inline: 0; bottom: 0; z-index: 100;
        padding: 1rem 1rem calc(1rem + env(safe-area-inset-bottom));
        border-radius: 1.5rem 1.5rem 0 0; background: var(--pos-sheet-bg);
        -webkit-backdrop-filter: saturate(1.6) blur(18px); backdrop-filter: saturate(1.6) blur(18px);
        box-shadow: 0 -1px 0 var(--pos-line), 0 -12px 32px -12px rgba(0, 0, 0, .25);
      }
    }

    /* Desktop: content in a card, pay column sticks while scrolling the people list */
    @media (min-width: 1024px) {
      .pos-page {
        padding: 2rem; border-radius: 1.5rem;
        background: var(--p-content-background, var(--pos-bg)); box-shadow: 0 0 0 1px var(--pos-line);
      }
      .pos-pay { position: sticky; top: 0; align-self: start; padding: 1.25rem; border-radius: 1.25rem; background: var(--pos-pay-bg); }
      .pos-pay .stepper, .pos-pay .split-chip:not([aria-pressed='true']) { background: var(--pos-bg); }
      .pos-pay .order-btn { min-height: 3.75rem; }
    }

    @media (prefers-reduced-motion: reduce) {
      *, .tile:active, .stepper-btn:active, .order-btn:active { transition: none !important; transform: none !important; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class PosComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly messageService = inject(MessageService);
  private readonly authService = inject(AuthService);
  private readonly popupService = inject(AchievementPopupService);
  private readonly currency = new CurrencyPipe(inject(LOCALE_ID));
  readonly ls = inject(LanguageService);

  private readonly document = inject(DOCUMENT);
  private readonly paySection = viewChild<ElementRef<HTMLElement>>('paySection');

  readonly minAmount = MIN_AMOUNT;
  readonly maxAmount = MAX_AMOUNT;
  readonly skeletonTiles = [1, 2, 3, 4, 5, 6];

  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);

  readonly products = signal<ProductDto[]>([]);
  readonly users = signal<ApplicationUserDto[]>([]);

  readonly selectedProducts = signal<string[]>([]);
  readonly selectedUsers = signal<string[]>([]);
  readonly amount = signal(MIN_AMOUNT);
  readonly split = signal(false);
  readonly userQuery = signal('');

  private readonly currentUser = computed(() => this.authService.currentUser());
  readonly isGuest = computed(() => this.currentUser()?.roles?.includes('Guest') ?? false);

  readonly selectedProductIds = computed(() => new Set(this.selectedProducts()));
  readonly selectedUserIds = computed(() => new Set(this.selectedUsers()));

  /** The logged-in user comes first: on a phone you mostly order for yourself. */
  private readonly sortedUsers = computed<PosUser[]>(() => {
    const me = this.currentUser()?.id;
    const users = this.users().map(u => {
      const displayName = u.fullName || u.userName;
      return { ...u, displayName, initials: initialsOf(displayName) };
    });
    const self = users.filter(u => u.id === me);
    return self.length ? [...self, ...users.filter(u => u.id !== me)] : users;
  });

  readonly showUserSearch = computed(() => this.users().length > USER_SEARCH_THRESHOLD);

  readonly visibleUsers = computed(() => {
    const query = this.userQuery().trim().toLowerCase();
    if (!query) {
      return this.sortedUsers();
    }
    return this.sortedUsers().filter(u =>
      u.displayName.toLowerCase().includes(query) || u.userName.toLowerCase().includes(query));
  });

  readonly canSplit = computed(() => this.selectedUsers().length > 1);

  /**
   * Cost preview, mirroring CreateOrderHandler: with split each person pays every product's
   * price divided by the number of people, rounded up to the cent, times the amount.
   */
  readonly summary = computed(() => {
    const userCount = this.selectedUsers().length;
    const ids = this.selectedProductIds();
    if (ids.size === 0 || userCount === 0) {
      return null;
    }
    const split = this.split() && userCount > 1;
    const perUserCents = this.products()
      .filter(p => ids.has(p.id))
      .reduce((sum, p) => {
        const cents = Math.round(p.price * 100);
        return sum + (split ? Math.ceil(cents / userCount) : cents);
      }, 0) * this.amount();
    return { perUser: perUserCents / 100, total: (perUserCents * userCount) / 100 };
  });

  /** Announced to screen readers whenever the price changes. */
  readonly summaryText = computed(() => {
    const s = this.summary();
    if (!s) {
      return this.ls.t().pos_select_hint;
    }
    const fmt = (v: number) => this.currency.transform(v, 'EUR', 'symbol', '1.2-2') ?? '';
    return this.selectedUsers().length > 1
      ? `${this.ls.t().total} ${fmt(s.total)}, ${fmt(s.perUser)} ${this.ls.t().per_person}`
      : `${this.ls.t().total} ${fmt(s.total)}`;
  });

  readonly canSubmit = computed(() =>
    !this.isSubmitting() && !this.isGuest() && this.summary() !== null);

  constructor() {
    // On phones the pay section is a fixed bottom sheet. Publish its height so the global
    // toast (styles.css, --app-bottom-offset) is shown above it instead of behind it.
    effect(onCleanup => {
      const el = this.paySection()?.nativeElement;
      if (!el || typeof ResizeObserver === 'undefined') {
        return;
      }
      const root = this.document.documentElement;
      const update = () => {
        const isSheet = getComputedStyle(el).position === 'fixed';
        root.style.setProperty('--app-bottom-offset', isSheet ? `${el.offsetHeight}px` : '0px');
      };
      const observer = new ResizeObserver(update);
      observer.observe(el);
      update();
      onCleanup(() => {
        observer.disconnect();
        root.style.removeProperty('--app-bottom-offset');
      });
    });
  }

  ngOnInit() {
    this.orderService.initialize().subscribe({
      next: (data) => {
        this.products.set(data.products || []);
        this.users.set(data.users || []);
        this.resetSelection();
        this.isLoading.set(false);
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        this.isLoading.set(false);
        console.error(err);
      }
    });
  }

  toggleProduct(id: string) {
    this.selectedProducts.update(ids => ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id]);
  }

  toggleUser(id: string) {
    this.selectedUsers.update(ids => ids.includes(id) ? ids.filter(x => x !== id) : [...ids, id]);
    if (this.selectedUsers().length < 2) {
      this.split.set(false);
    }
  }

  changeAmount(delta: number) {
    this.setAmount(this.amount() + delta);
  }

  /** Accepts free input (e.g. "2.5", "", "300") and clamps it to a whole number in range. */
  setAmount(value: number | string) {
    const parsed = Math.trunc(Number(value));
    this.amount.set(Number.isFinite(parsed) ? Math.min(MAX_AMOUNT, Math.max(MIN_AMOUNT, parsed)) : MIN_AMOUNT);
  }

  submitOrder() {
    if (!this.canSubmit()) {
      return;
    }

    this.isSubmitting.set(true);

    this.orderService.submit({
      products: this.selectedProducts(),
      users: this.selectedUsers(),
      amount: this.amount(),
      split: this.split() && this.selectedUsers().length > 1
    }).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().order_placed });
        this.authService.refreshCurrentUser();
        this.isSubmitting.set(false);

        // Check for newly acquired achievements after placing order
        this.popupService.checkUnseenAchievements();

        this.selectedProducts.set([]);
        this.resetSelection();
        this.amount.set(MIN_AMOUNT);
        this.split.set(false);
        this.userQuery.set('');
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().order_failed });
        this.isSubmitting.set(false);
        console.error(err);
      }
    });
  }

  /** Preselect yourself (not on a shared kiosk account). */
  private resetSelection() {
    const user = this.currentUser();
    const isKiosk = user?.roles?.includes('Kiosk') ?? false;
    if (user?.id && !isKiosk && this.users().some(u => u.id === user.id)) {
      this.selectedUsers.set([user.id]);
    } else {
      this.selectedUsers.set([]);
    }
  }
}
