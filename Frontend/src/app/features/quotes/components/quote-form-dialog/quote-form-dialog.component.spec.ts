import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { vi } from 'vitest';
import { QuoteFormDialogComponent } from './quote-form-dialog.component';
import { ApplicationUserService } from '../../../users/services/application-user.service';
import { RoleService } from '../../../admin/services/role.service';
import { GlobalSettingsService } from '../../../admin/services/global-settings.service';

describe('QuoteFormDialogComponent', () => {
  let component: QuoteFormDialogComponent;
  let ref: { close: ReturnType<typeof vi.fn> };
  let settingsService: any;

  const users = [
    { id: 'u1', fullName: 'Jan Jansen', userName: 'jan' },
    { id: 'u2', fullName: 'Piet Pietersen', userName: 'piet' },
  ];
  const roles = [{ name: 'Leiding' }, { name: 'Bestuur' }, { name: 'Ouders' }];

  function create(data: any = {}) {
    TestBed.configureTestingModule({
      imports: [QuoteFormDialogComponent],
      providers: [
        { provide: DynamicDialogRef, useValue: ref },
        { provide: DynamicDialogConfig, useValue: { data } },
        { provide: ApplicationUserService, useValue: { getUsers: () => of({ items: users, totalCount: 2 }) } },
        { provide: RoleService, useValue: { getAllRoles: () => of(roles) } },
        { provide: GlobalSettingsService, useValue: settingsService },
      ],
    });
    TestBed.overrideComponent(QuoteFormDialogComponent, { set: { template: '' } });
    const fixture = TestBed.createComponent(QuoteFormDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    ref = { close: vi.fn() };
    settingsService = { getSetting: vi.fn(() => of({ value: 'Leiding, Bestuur' })) };
  });

  it('loads users and the roles quotes can be shown to', () => {
    create();
    expect(component.isEdit).toBe(false);
    expect(component.availableUsers()).toEqual(users);
    expect(component.availableRoles()).toEqual([{ name: 'Leiding' }, { name: 'Bestuur' }]);
  });

  it('offers all roles when the setting is empty or missing', () => {
    settingsService.getSetting.mockReturnValue(of({ value: '' }));
    create();
    expect(component.availableRoles()).toEqual(roles);

    TestBed.resetTestingModule();
    settingsService.getSetting.mockReturnValue(throwError(() => new Error('404')));
    create();
    expect(component.availableRoles()).toEqual(roles);
  });

  it('fills the form when editing a quote', () => {
    create({
      quote: {
        id: 'q1',
        text: 'Hoi',
        dateSaid: '2026-01-02T00:00:00Z',
        visibleToRoles: ['Leiding'],
        authors: [
          { applicationUserId: 'u1', applicationUser: users[0] },
          { applicationUserId: null, customName: 'Opa' },
          { applicationUserId: null },
        ],
      },
    });

    expect(component.isEdit).toBe(true);
    expect(component.quoteId).toBe('q1');
    expect(component.quoteForm.value.text).toBe('Hoi');
    expect(component.selectedAuthors().map((a) => a.name)).toEqual(['Jan Jansen', 'Opa', 'Unknown']);
  });

  it('suggests matching users and a custom author', () => {
    create();
    component.searchAuthors({ query: 'jan' } as any);
    expect(component.filteredAuthors()).toEqual([
      { id: 'u1', name: 'Jan Jansen', isUser: true, user: users[0] },
      { name: 'jan', isUser: false, isNew: true },
    ]);

    component.searchAuthors({ query: 'Jan Jansen' } as any);
    expect(component.filteredAuthors()).toHaveLength(1);
  });

  it('adds authors once and removes them', () => {
    create();
    component.addAuthor({ value: { id: 'u1', name: 'Jan Jansen', isUser: true, user: users[0] } });
    component.authorInput = 'Jan';
    component.addAuthor({ value: { id: 'u1', name: 'Jan Jansen', isUser: true } });
    expect(component.selectedAuthors()).toHaveLength(1);
    expect(component.authorInput).toBe('');

    component.removeAuthor(component.selectedAuthors()[0]);
    expect(component.selectedAuthors()).toEqual([]);
  });

  it('submits the quote with its authors', () => {
    create();
    component.onSubmit();
    expect(ref.close).not.toHaveBeenCalled();

    component.quoteForm.patchValue({ text: 'Hoi', dateSaid: new Date('2026-01-02T00:00:00Z'), sendNotification: true });
    component.addAuthor({ value: { id: 'u1', name: 'Jan Jansen', isUser: true } });
    component.addAuthor({ value: { name: 'Opa', isUser: false } });
    component.onSubmit();

    expect(ref.close).toHaveBeenCalledWith({
      text: 'Hoi',
      dateSaid: '2026-01-02T00:00:00.000Z',
      authors: [
        { applicationUserId: 'u1', customName: null },
        { applicationUserId: null, customName: 'Opa' },
      ],
      visibleToRoles: [],
      sendNotification: true,
    });
  });

  it('closes without a result on cancel', () => {
    create();
    component.onCancel();
    expect(ref.close).toHaveBeenCalledWith();
  });
});
