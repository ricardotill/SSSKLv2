import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { QuoteCardComponent } from './quote-card.component';
import { QuoteService } from '../../services/quote.service';
import { UrlService } from '../../../../core/services/url.service';
import { UserProfileDrawerService } from '../../../../core/services/user-profile-drawer.service';

describe('QuoteCardComponent (rendered)', () => {
  let fixture: ComponentFixture<QuoteCardComponent>;
  let component: QuoteCardComponent;
  let router: { navigate: ReturnType<typeof vi.fn> };
  let drawer: { open: ReturnType<typeof vi.fn> };

  const quote = (hasVoted: boolean) =>
    ({
      id: 'q1',
      text: 'Dat is geen bug',
      dateSaid: '2026-01-01T00:00:00Z',
      hasVoted,
      voteCount: 3,
      commentsCount: 2,
      authors: [
        { id: 'a1', applicationUserId: 'u1', applicationUser: { fullName: 'Jan Jansen', profilePictureUrl: '/pic/u1' } },
        { id: 'a2', applicationUserId: 'u2', applicationUser: { fullName: 'Piet', profilePictureUrl: null } },
        { id: 'a3', applicationUserId: null, customName: 'Oom Kees' },
      ],
    }) as any;

  const el = () => fixture.nativeElement as HTMLElement;

  function create(hasVoted: boolean, canEdit: boolean) {
    router = { navigate: vi.fn() };
    drawer = { open: vi.fn() };
    TestBed.configureTestingModule({
      imports: [QuoteCardComponent],
      providers: [
        { provide: Router, useValue: router },
        { provide: QuoteService, useValue: { toggleVote: vi.fn(() => of(void 0)) } },
        { provide: UrlService, useValue: { resolveApiUrl: (u: string | null | undefined) => (u ? `https://api${u}` : undefined) } },
        { provide: UserProfileDrawerService, useValue: drawer },
      ],
    });
    fixture = TestBed.createComponent(QuoteCardComponent);
    component = fixture.componentInstance;
    component.quote = quote(hasVoted);
    component.canEdit = canEdit;
    component.canDelete = canEdit;
    fixture.detectChanges();
  }

  it('renders the quote with linked and custom authors', () => {
    create(true, true);

    expect(el().textContent).toContain('"Dat is geen bug"');
    expect(el().textContent).toContain('Jan Jansen');
    expect(el().textContent).toContain('Oom Kees');
    expect(el().querySelectorAll('p-button')).toHaveLength(2);
    expect(el().querySelector('.pi-heart-fill')).toBeTruthy();
  });

  it('opens an author profile and runs the card actions', () => {
    create(false, true);
    const edit = vi.fn();
    const remove = vi.fn();
    component.onEdit.subscribe(edit);
    component.onDelete.subscribe(remove);

    el().querySelector<HTMLElement>('.group\\/author')!.click();
    expect(drawer.open).toHaveBeenCalledWith('u1');
    expect(router.navigate).not.toHaveBeenCalled();

    el().querySelector<HTMLButtonElement>('p-button[icon="pi pi-pencil"] button')!.click();
    el().querySelector<HTMLButtonElement>('p-button[icon="pi pi-trash"] button')!.click();
    expect(edit).toHaveBeenCalled();
    expect(remove).toHaveBeenCalled();
  });

  it('hides the actions for readers', () => {
    create(false, false);
    expect(el().querySelectorAll('p-button')).toHaveLength(0);
    expect(el().querySelector('.pi-heart')).toBeTruthy();
  });
});
