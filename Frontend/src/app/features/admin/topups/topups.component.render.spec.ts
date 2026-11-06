import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import TopUpsComponent from './topups.component';
import { TopUpService } from '../../top-ups/services/top-up.service';
import { ApplicationUserService } from '../../users/services/application-user.service';
import { AuthService } from '../../../core/auth/auth.service';
import { LanguageService } from '../../../core/services/language.service';

describe('TopUpsComponent (rendered, mobile layout)', () => {
  const row: any = { id: 't1', userName: 'jan', saldo: 10 };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TopUpsComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: TopUpService, useValue: { getTopUps: vi.fn(() => of({ items: [row], totalCount: 1 })) } },
        { provide: ApplicationUserService, useValue: { getUsers: vi.fn(() => of([])) } },
        { provide: AuthService, useValue: { refreshCurrentUser: vi.fn() } },
      ],
    }).compileComponents();
  });

  it('marks the table as stackable and labels every body cell with its column header', async () => {
    const fixture = TestBed.createComponent(TopUpsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const ls = TestBed.inject(LanguageService);
    const t = ls.t() as Record<string, string>;

    expect(el.querySelector('p-table.responsive-stack-table')).not.toBeNull();
    expect(el.textContent).toContain('jan');
    const labels = Array.from(el.querySelectorAll('tbody tr:first-child td')).map((td) => td.getAttribute('data-label'));
    expect(labels).toEqual(['username', 'amount', 'actions'].map((k) => t[k]));
  });
});
