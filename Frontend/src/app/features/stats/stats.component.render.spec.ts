import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import { StatsComponent } from './stats.component';
import { AuthService } from '../../core/auth/auth.service';
import { AchievementService } from '../achievements/services/achievement.service';
import { ApplicationUserService } from '../users/services/application-user.service';
import { UrlService } from '../../core/services/url.service';

describe('StatsComponent (rendered)', () => {
  let fixture: ComponentFixture<StatsComponent>;
  let userService: any;
  let entries: any[];
  let query: BehaviorSubject<any>;
  let roles: string[];

  const stats = {
    totalOrders: 4,
    totalItemsBought: 9,
    totalSpent: 12.5,
    totalTopUp: 20,
    maxSingleTopUp: 10,
    maxOrdersPerHour: 2,
    quoteCount: 1,
    quoteVotesGiven: 2,
    quoteVotesReceived: 3,
    reactionCount: 4,
    currentStreak: 3,
    membershipStartDate: '2025-01-01T00:00:00Z',
    minMinutesBetweenOrders: 5,
    minMinutesBetweenTopUp: 7,
    lastActivityDate: '2026-01-01T10:00:00Z',
    lastOrderDate: '0001-01-01T00:00:00',
    lastTopUpDate: null,
    lastStatsRecalculatedAt: '2026-01-02T10:00:00Z',
  } as any;

  function create() {
    TestBed.configureTestingModule({
      imports: [StatsComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { currentUser: signal<any>({ id: 'me', roles }) } },
        { provide: ApplicationUserService, useValue: userService },
        { provide: AchievementService, useValue: { getAchievementEntries: vi.fn(() => of(entries)) } },
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: UrlService, useValue: { resolveApiUrl: (u: string | null | undefined) => (u ? `https://api${u}` : undefined) } },
        { provide: ActivatedRoute, useValue: { queryParamMap: query, snapshot: { queryParamMap: query.value } } },
      ],
    });
    fixture = TestBed.createComponent(StatsComponent);
    fixture.detectChanges();
  }

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  beforeEach(() => {
    userService = {
      getUserStats: vi.fn(() => of(stats)),
      getUser: vi.fn(() => of({ fullName: 'Jan Jansen' })),
      recalculateUserStats: vi.fn(() => of(stats)),
    };
    entries = [
      { id: 'e1', achievementId: 'a1', achievementName: 'Met plaatje', imageUrl: '/img/a1' },
      { id: 'e2', achievementId: 'a2', achievementName: 'Zonder plaatje', imageUrl: null },
    ];
    query = new BehaviorSubject(convertToParamMap({}));
    roles = ['User'];
  });

  it('renders the own stats with records and achievements', () => {
    create();

    expect(text()).toContain('Mijn statistieken');
    expect(text()).toContain('Laatste herberekening');
    expect(text()).toContain('Totale bestellingen');
    expect(text()).toContain('Gebeurtenis heeft nog niet plaatsgevonden');
    expect(text()).toContain('Met plaatje');
    expect(text()).toContain('Zonder plaatje');
    expect((fixture.nativeElement as HTMLElement).querySelector('img')?.getAttribute('src')).toBe('https://api/img/a1');
  });

  it('renders the stats of another user without achievements', () => {
    entries = [];
    roles = ['Admin'];
    query.next(convertToParamMap({ userId: 'other' }));
    create();

    expect(text()).toContain('Gebruikersprofiel');
    expect(text()).toContain('Statistieken van Jan Jansen');
    expect(text()).toContain('Nog geen achievements behaald.');
  });

  it('renders a message when the stats could not be loaded', () => {
    userService.getUserStats.mockImplementation(() => throwError(() => new Error('x')));
    create();

    expect(text()).toContain('Statistieken konden niet worden geladen.');
  });
});
