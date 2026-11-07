import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import PosComponent from './pos.component';
import { OrderService } from '../orders/services/order.service';
import { AuthService } from '../../core/auth/auth.service';
import { AchievementPopupService } from '../achievements/services/achievement-popup.service';

describe('PosComponent', () => {
  let component: PosComponent;
  let orderService: { initialize: ReturnType<typeof vi.fn>; submit: ReturnType<typeof vi.fn> };
  let messageService: { add: ReturnType<typeof vi.fn> };
  let currentUser: ReturnType<typeof signal<any>>;

  const products = [
    { id: 'p1', name: 'Biertje', price: 1.5 },
    { id: 'p2', name: 'Fris', price: 1.0 },
  ];
  const users = [
    { id: 'u1', userName: 'alice', fullName: 'Alice de Vries' },
    { id: 'u2', userName: 'bob', fullName: 'Bob Janssen' },
    { id: 'me', userName: 'ricardo', fullName: 'Ricardo' },
  ];

  function create(options: { render?: boolean } = {}) {
    TestBed.configureTestingModule({
      imports: [PosComponent],
      providers: [
        { provide: OrderService, useValue: orderService },
        { provide: MessageService, useValue: messageService },
        { provide: AuthService, useValue: { currentUser, refreshCurrentUser: vi.fn() } },
        { provide: AchievementPopupService, useValue: { checkUnseenAchievements: vi.fn() } },
      ],
    });
    if (!options.render) {
      TestBed.overrideComponent(PosComponent, { set: { template: '' } });
    }
    const fixture = TestBed.createComponent(PosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    orderService = {
      initialize: vi.fn(() => of({ products, users })),
      submit: vi.fn(() => of(void 0)),
    };
    messageService = { add: vi.fn() };
    currentUser = signal<any>({ id: 'me', roles: ['User'] });
  });

  it('preselects and lists the current user first', () => {
    create();
    expect(component.selectedUsers()).toEqual(['me']);
    expect(component.visibleUsers()[0].id).toBe('me');
  });

  it('does not preselect anyone for a kiosk account', () => {
    currentUser.set({ id: 'me', roles: ['Kiosk'] });
    create();
    expect(component.selectedUsers()).toEqual([]);
  });

  it('filters people by full name or username', () => {
    create();
    component.userQuery.set('JANS');
    expect(component.visibleUsers().map(u => u.id)).toEqual(['u2']);
    component.userQuery.set('alice');
    expect(component.visibleUsers().map(u => u.id)).toEqual(['u1']);
  });

  it('has no summary until a product and a person are selected', () => {
    create();
    expect(component.summary()).toBeNull();
    expect(component.canSubmit()).toBe(false);
    component.toggleProduct('p1');
    expect(component.canSubmit()).toBe(true);
  });

  it('computes the price per person and total without split', () => {
    create();
    component.toggleProduct('p1');
    component.toggleProduct('p2');
    component.toggleUser('u1');
    component.changeAmount(1);
    expect(component.summary()).toEqual({ perUser: 5, total: 10 });
  });

  it('rounds split shares up to the cent like the backend', () => {
    create();
    component.toggleProduct('p2');
    component.toggleUser('u1');
    component.toggleUser('u2');
    component.split.set(true);
    // €1,00 / 3 = €0,34 each
    expect(component.summary()).toEqual({ perUser: 0.34, total: 1.02 });
  });

  it('turns split off when fewer than two people remain', () => {
    create();
    component.toggleUser('u1');
    component.split.set(true);
    component.toggleUser('u1');
    expect(component.canSplit()).toBe(false);
    expect(component.split()).toBe(false);
  });

  it('clamps the amount to a whole number between 1 and 99', () => {
    create();
    component.setAmount('2.7');
    expect(component.amount()).toBe(2);
    component.setAmount('');
    expect(component.amount()).toBe(1);
    component.setAmount('500');
    expect(component.amount()).toBe(99);
    component.setAmount('abc');
    expect(component.amount()).toBe(1);
    component.changeAmount(-1);
    expect(component.amount()).toBe(1);
  });

  it('does not let guests order', () => {
    currentUser.set({ id: 'me', roles: ['Guest'] });
    create();
    component.toggleProduct('p1');
    component.submitOrder();
    expect(orderService.submit).not.toHaveBeenCalled();
  });

  it('submits the order and resets the form', () => {
    create();
    component.toggleProduct('p1');
    component.toggleUser('u1');
    component.split.set(true);
    component.changeAmount(2);
    component.userQuery.set('ali');

    component.submitOrder();

    expect(orderService.submit).toHaveBeenCalledWith({ products: ['p1'], users: ['me', 'u1'], amount: 3, split: true });
    expect(component.selectedProducts()).toEqual([]);
    expect(component.selectedUsers()).toEqual(['me']);
    expect(component.amount()).toBe(1);
    expect(component.split()).toBe(false);
    expect(component.userQuery()).toBe('');
  });

  it('keeps the selection when ordering fails', () => {
    orderService.submit.mockReturnValue(throwError(() => new Error('boom')));
    vi.spyOn(console, 'error').mockImplementation(() => {});
    create();
    component.toggleProduct('p1');
    component.submitOrder();
    expect(component.selectedProducts()).toEqual(['p1']);
    expect(component.isSubmitting()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('renders one pay section with the total in the order button', () => {
    const fixture = create({ render: true });
    component.toggleProduct('p1');
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelectorAll('.pos-pay')).toHaveLength(1);
    expect(el.querySelector('.pos-pay .order-btn')?.textContent).toContain('1.50');
  });
});
