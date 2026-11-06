import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import ProductsComponent from './products.component';
import { ProductService } from '../../pos/services/product.service';
import { LanguageService } from '../../../core/services/language.service';

describe('ProductsComponent (rendered, mobile layout)', () => {
  const row: any = { id: 'p1', name: 'Cola', description: 'Koud', price: 1.5, stock: 10, enableLeaderboard: true };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductsComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: ProductService, useValue: { getProducts: vi.fn(() => of({ items: [row], totalCount: 1 })) } },
      ],
    }).compileComponents();
  });

  it('marks the table as stackable and labels every body cell with its column header', async () => {
    const fixture = TestBed.createComponent(ProductsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const ls = TestBed.inject(LanguageService);
    const t = ls.t() as Record<string, string>;

    expect(el.querySelector('p-table.responsive-stack-table')).not.toBeNull();
    expect(el.textContent).toContain('Cola');
    const labels = Array.from(el.querySelectorAll('tbody tr:first-child td')).map((td) => td.getAttribute('data-label'));
    expect(labels).toEqual(['product_name', 'product_description', 'price', 'stock', 'leaderboard', 'actions'].map((k) => t[k]));
  });
});
