import { Component, ChangeDetectionStrategy, DOCUMENT, inject, signal } from '@angular/core';
import { of, delay } from 'rxjs';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import PosComponent from './pos.component';
import { OrderService } from '../orders/services/order.service';
import { AuthService } from '../../core/auth/auth.service';
import { AchievementPopupService } from '../achievements/services/achievement-popup.service';
import { ApplicationUserDto } from '../../core/models/application-user.model';
import { ProductDto } from '../../core/models/product.model';
import { OrderSubmitDto } from '../../core/models/order.model';

// Dev-only harness (route `pos/dev`, registered only in dev mode): renders the real POS page
// with fake data so the layout can be checked without a backend or login.
// Append `?dark` to the URL for dark mode.

const FAKE_PRODUCTS: ProductDto[] = [
  { id: 'p1', name: 'Biertje', description: null, price: 1.5, stock: 99, enableLeaderboard: true },
  { id: 'p2', name: 'Fris', description: null, price: 1.0, stock: 99, enableLeaderboard: false },
  { id: 'p3', name: 'Wijn', description: null, price: 2.0, stock: 99, enableLeaderboard: true },
  { id: 'p4', name: 'Koffie', description: null, price: 1.25, stock: 99, enableLeaderboard: false },
  { id: 'p5', name: 'Speciaalbier', description: null, price: 2.75, stock: 99, enableLeaderboard: true },
];

const NAMES = [
  'Ricardo Tillemans', 'Alice de Vries', 'Bob Janssen', 'Charlie Bakker', 'Diana van den Berg', 'Eve Smits',
  'Frank Mulder', 'Grace Visser', 'Hank de Groot', 'Iris Meijer', 'Jan Peters', 'Karen van Dijk',
  'Lars Bos', 'Maria Hendriks', 'Niels Kuiper', 'Olivia Kok',
];

const FAKE_USERS: ApplicationUserDto[] = NAMES.map((fullName, i) => ({
  id: i === 0 ? 'me' : `u${i}`,
  userName: fullName.split(' ')[0].toLowerCase(),
  fullName,
  saldo: 10,
  roles: ['User'],
}));

@Component({
  selector: 'app-pos-demo',
  imports: [PosComponent, ToastModule],
  providers: [
    MessageService,
    {
      provide: OrderService,
      useValue: {
        initialize: () => of({ products: FAKE_PRODUCTS, users: FAKE_USERS }).pipe(delay(300)),
        submit: (order: OrderSubmitDto) => {
          console.info('[pos/dev] order', order);
          return of(void 0).pipe(delay(600));
        },
      },
    },
    { provide: AchievementPopupService, useValue: { checkUnseenAchievements: () => undefined } },
    // A regular member is "logged in", so they get preselected like in production.
    { provide: AuthService, useValue: { currentUser: signal({ id: 'me', roles: ['User'] }), refreshCurrentUser: () => undefined } },
  ],
  template: `
    <p-toast position="bottom-left" />
    <div class="min-h-screen bg-surface-50 dark:bg-surface-950 p-4 md:p-8">
      <app-pos />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PosDemoComponent {
  constructor() {
    const document = inject(DOCUMENT);
    if (new URLSearchParams(document.location.search).has('dark')) {
      document.documentElement.classList.add('dark');
    }
  }
}
