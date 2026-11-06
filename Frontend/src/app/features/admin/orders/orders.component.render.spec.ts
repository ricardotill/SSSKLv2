import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import OrdersComponent from './orders.component';
import { OrderService } from '../../orders/services/order.service';
import { AuthService } from '../../../core/auth/auth.service';
import { LanguageService } from '../../../core/services/language.service';

describe('OrdersComponent (rendered, mobile layout)', () => {
  const row: any = { id: 'o1', createdOn: '2026-01-01T10:00:00Z', userFullName: 'Jan Jansen', productName: 'Cola', amount: 2, paid: 3 };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrdersComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: OrderService, useValue: { getOrders: vi.fn(() => of({ items: [row], totalCount: 1 })) } },
        { provide: AuthService, useValue: { refreshCurrentUser: vi.fn() } },
      ],
    }).compileComponents();
  });

  it('marks the table as stackable and labels every body cell with its column header', async () => {
    const fixture = TestBed.createComponent(OrdersComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const ls = TestBed.inject(LanguageService);
    const t = ls.t() as Record<string, string>;

    expect(el.querySelector('p-table.responsive-stack-table')).not.toBeNull();
    expect(el.textContent).toContain('Jan Jansen');
    const labels = Array.from(el.querySelectorAll('tbody tr:first-child td')).map((td) => td.getAttribute('data-label'));
    expect(labels).toEqual(['date', 'user', 'product', 'amount', 'price', 'actions'].map((k) => t[k]));
  });
});
