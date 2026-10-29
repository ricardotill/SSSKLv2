import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, Subject, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { vi } from 'vitest';
import QuotesComponent from './quotes.component';
import { QuoteService } from './services/quote.service';
import { AuthService } from '../../core/auth/auth.service';
import { GlobalSettingsService } from '../admin/services/global-settings.service';
import { QuoteDto } from '../../core/models/quote.model';

describe('QuotesComponent', () => {
  let component: QuotesComponent;
  let quoteService: any;
  let settingsService: any;
  let messageService: { add: ReturnType<typeof vi.fn> };
  let dialogService: { open: ReturnType<typeof vi.fn> };
  let dialogClose: Subject<any>;
  let currentUser: ReturnType<typeof signal<any>>;

  const quote = {
    id: 'q1',
    text: 'Hoi',
    createdBy: { id: 'creator' },
    authors: [{ applicationUserId: 'author' }],
  } as unknown as QuoteDto;

  function create() {
    TestBed.configureTestingModule({
      imports: [QuotesComponent],
      providers: [
        { provide: QuoteService, useValue: quoteService },
        { provide: AuthService, useValue: { currentUser } },
        { provide: GlobalSettingsService, useValue: settingsService },
        { provide: MessageService, useValue: messageService },
      ],
    });
    TestBed.overrideComponent(QuotesComponent, {
      set: { template: '', providers: [{ provide: DialogService, useValue: dialogService }] },
    });
    const fixture = TestBed.createComponent(QuotesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    quoteService = {
      getQuotes: vi.fn(() => of({ items: [quote], totalCount: 1 })),
      createQuote: vi.fn(() => of({})),
      updateQuote: vi.fn(() => of({})),
      deleteQuote: vi.fn(() => of(void 0)),
    };
    settingsService = { getSetting: vi.fn(() => of({ value: 'Leiding, Bestuur' })) };
    messageService = { add: vi.fn() };
    dialogClose = new Subject();
    dialogService = { open: vi.fn(() => ({ onClose: dialogClose })) };
    currentUser = signal<any>({ id: 'u1', roles: ['User'] });
  });

  afterEach(() => vi.unstubAllGlobals());

  it('loads the allowed roles and limits who can create quotes', () => {
    create();
    expect(component.allowedRoles()).toEqual(['Leiding', 'Bestuur']);
    expect(component.canCreate()).toBe(false);

    currentUser.set({ id: 'u1', roles: ['Bestuur'] });
    expect(component.canCreate()).toBe(true);

    currentUser.set({ id: 'u1', roles: ['Admin'] });
    expect(component.canCreate()).toBe(true);

    currentUser.set(null);
    expect(component.canCreate()).toBe(false);
  });

  it('lets everyone create quotes when no roles are configured', () => {
    settingsService.getSetting.mockReturnValue(of({ value: '' }));
    create();
    expect(component.canCreate()).toBe(true);
  });

  it('keeps the defaults when the setting is missing', () => {
    const log = vi.spyOn(console, 'log').mockImplementation(() => undefined);
    settingsService.getSetting.mockReturnValue(throwError(() => new Error('404')));
    create();
    expect(component.allowedRoles()).toEqual([]);
    expect(log).toHaveBeenCalled();
  });

  it('loads a page of quotes', () => {
    create();
    component.onLazyLoad({ first: 12, rows: 24 } as any);
    expect(quoteService.getQuotes).toHaveBeenCalledWith(12, 24);
    expect(component.quotes()).toEqual([quote]);
    expect(component.totalRecords()).toBe(1);
    expect(component.loading()).toBe(false);

    component.onLazyLoad({} as any);
    expect(quoteService.getQuotes).toHaveBeenCalledWith(0, 12);
  });

  it('stops loading when quotes fail to load', () => {
    quoteService.getQuotes.mockReturnValue(throwError(() => new Error('x')));
    create();
    component.loadQuotes();
    expect(component.loading()).toBe(false);
  });

  it('lets admins, creators and authors edit and delete', () => {
    create();
    expect(component.canEdit(quote)).toBe(false);
    expect(component.canDelete(quote)).toBe(false);

    currentUser.set({ id: 'creator', roles: [] });
    expect(component.canEdit(quote)).toBe(true);

    currentUser.set({ id: 'author', roles: [] });
    expect(component.canDelete(quote)).toBe(true);

    currentUser.set({ id: 'x', roles: ['Admin'] });
    expect(component.canEdit(quote)).toBe(true);

    currentUser.set(null);
    expect(component.canEdit(quote)).toBe(false);
  });

  it('creates a quote from the dialog', () => {
    create();
    component.openQuoteForm();
    expect(dialogService.open).toHaveBeenCalledWith(expect.anything(), expect.objectContaining({ header: 'Add Quote' }));

    dialogClose.next({ text: 'Nieuw' });

    expect(quoteService.createQuote).toHaveBeenCalledWith({ text: 'Nieuw' });
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('updates a quote from the dialog', () => {
    create();
    component.openQuoteForm(quote);
    expect(dialogService.open).toHaveBeenCalledWith(expect.anything(), expect.objectContaining({ header: 'Edit Quote' }));

    dialogClose.next({ text: 'Aangepast' });

    expect(quoteService.updateQuote).toHaveBeenCalledWith('q1', { text: 'Aangepast' });
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('reports failed saves and ignores a cancelled dialog', () => {
    quoteService.createQuote.mockReturnValue(throwError(() => new Error('x')));
    quoteService.updateQuote.mockReturnValue(throwError(() => new Error('x')));
    create();

    component.openQuoteForm();
    dialogClose.next(undefined);
    expect(quoteService.createQuote).not.toHaveBeenCalled();

    dialogClose.next({ text: 'a' });
    component.openQuoteForm(quote);
    dialogClose.next({ text: 'b' });

    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('deletes a quote after confirmation', () => {
    create();
    vi.stubGlobal('confirm', vi.fn(() => false));
    component.confirmDelete(quote);
    expect(quoteService.deleteQuote).not.toHaveBeenCalled();

    vi.stubGlobal('confirm', vi.fn(() => true));
    component.confirmDelete(quote);
    expect(quoteService.deleteQuote).toHaveBeenCalledWith('q1');
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));

    quoteService.deleteQuote.mockReturnValue(throwError(() => new Error('x')));
    component.confirmDelete(quote);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });
});
