import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import PersonalOrdersComponent from './personal-orders.component';
import { OrderService } from './services/order.service';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/services/language.service';

describe('PersonalOrdersComponent (rendered, mobile layout)', () => {
  const row: any = { id: 'o1', createdOn: '2026-01-01T10:00:00Z', productName: 'Cola', amount: 2, paid: 3 };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PersonalOrdersComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: OrderService, useValue: { getPersonalOrders: vi.fn(() => of({ items: [row], totalCount: 1 })) } },
        { provide: AuthService, useValue: { refreshCurrentUser: vi.fn() } },
      ],
    }).compileComponents();
  });

  it('marks the table as stackable and labels every body cell with its column header', async () => {
    const fixture = TestBed.createComponent(PersonalOrdersComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const ls = TestBed.inject(LanguageService);
    const t = ls.t() as Record<string, string>;

    expect(el.querySelector('p-table.responsive-stack-table')).not.toBeNull();
    expect(el.textContent).toContain('Cola');
    const labels = Array.from(el.querySelectorAll('tbody tr:first-child td')).map((td) => td.getAttribute('data-label'));
    expect(labels).toEqual(['date', 'product', 'amount', 'price', 'actions'].map((k) => t[k]));
  });
});
