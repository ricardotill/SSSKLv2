import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import { StatsComponent } from './stats.component';
import { AuthService } from '../../core/auth/auth.service';
import { AchievementService } from '../achievements/services/achievement.service';
import { ApplicationUserService } from '../users/services/application-user.service';

describe('StatsComponent', () => {
  let component: StatsComponent;
  let userService: any;
  let messageService: { add: ReturnType<typeof vi.fn> };
  let currentUser: ReturnType<typeof signal<any>>;
  let query: BehaviorSubject<any>;

  const stats = { totalOrders: 4, totalSpent: 12.5, currentStreak: 3, lastStatsRecalculatedAt: undefined } as any;

  function create() {
    TestBed.configureTestingModule({
      imports: [StatsComponent],
      providers: [
        { provide: AuthService, useValue: { currentUser } },
        { provide: ApplicationUserService, useValue: userService },
        { provide: AchievementService, useValue: { getAchievementEntries: vi.fn(() => of([{ id: 'a' }])) } },
        { provide: MessageService, useValue: messageService },
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: query, snapshot: { queryParamMap: query.value } },
        },
      ],
    });
    TestBed.overrideComponent(StatsComponent, { set: { template: '' } });
    const fixture = TestBed.createComponent(StatsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(() => {
    userService = {
      getUserStats: vi.fn(() => of(stats)),
      getUser: vi.fn(() => of({ fullName: 'Jan Jansen' })),
      recalculateUserStats: vi.fn(() => of({ ...stats, totalOrders: 5 })),
    };
    messageService = { add: vi.fn() };
    currentUser = signal<any>({ id: 'me', roles: ['User'] });
    query = new BehaviorSubject(convertToParamMap({}));
  });

  it('loads the stats of the current user', () => {
    create();
    expect(userService.getUserStats).toHaveBeenCalledWith('me');
    expect(component.stats()).toEqual(stats);
    expect(component.achievements()).toHaveLength(1);
    expect(component.viewingOwnStats()).toBe(true);
    expect(component.loading()).toBe(false);
    expect(userService.getUser).not.toHaveBeenCalled();
  });

  it('ignores a requested user for non-admins', () => {
    query.next(convertToParamMap({ userId: 'other' }));
    create();
    expect(userService.getUserStats).toHaveBeenCalledWith('me');
  });

  it('lets admins view another user', () => {
    currentUser.set({ id: 'me', roles: ['Admin'] });
    query.next(convertToParamMap({ userId: 'other' }));
    create();
    expect(userService.getUserStats).toHaveBeenCalledWith('other');
    expect(component.viewingOwnStats()).toBe(false);
    expect(component.displayedUserName()).toBe('Jan Jansen');
  });

  it('does nothing without a signed-in user', () => {
    currentUser.set(null);
    create();
    expect(userService.getUserStats).not.toHaveBeenCalled();
  });

  it('stops loading when stats fail to load', () => {
    userService.getUserStats.mockReturnValue(throwError(() => new Error('x')));
    TestBed.overrideProvider(AchievementService, {
      useValue: { getAchievementEntries: () => throwError(() => new Error('x')) },
    });
    create();
    expect(component.loading()).toBe(false);
    expect(component.achievements()).toEqual([]);
  });

  it('limits recalculation to once a week for non-admins', () => {
    create();
    expect(component.canRecalculate()).toBe(true);

    component.stats.set({ ...stats, lastStatsRecalculatedAt: new Date().toISOString() });
    expect(component.canRecalculate()).toBe(false);
    component.recalculateStats();
    expect(userService.recalculateUserStats).not.toHaveBeenCalled();

    component.stats.set({ ...stats, lastStatsRecalculatedAt: '2020-01-01T00:00:00Z' });
    expect(component.canRecalculate()).toBe(true);

    currentUser.set({ id: 'me', roles: ['Admin'] });
    component.stats.set({ ...stats, lastStatsRecalculatedAt: new Date().toISOString() });
    expect(component.canRecalculate()).toBe(true);
  });

  it('recalculates stats', () => {
    create();
    component.recalculateStats();
    expect(userService.recalculateUserStats).toHaveBeenCalledWith('me');
    expect(component.stats()?.totalOrders).toBe(5);
    expect(component.recalculating()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('explains a rate-limited or failed recalculation', () => {
    create();
    userService.recalculateUserStats.mockReturnValue(throwError(() => ({ status: 429 })));
    component.recalculateStats();
    expect(messageService.add).toHaveBeenCalledWith(
      expect.objectContaining({ detail: 'Je kunt statistieken maar eenmaal per zeven dagen herberekenen.' }),
    );

    userService.recalculateUserStats.mockReturnValue(throwError(() => ({ status: 500 })));
    component.recalculateStats();
    expect(messageService.add).toHaveBeenCalledWith(
      expect.objectContaining({ detail: 'Statistieken konden niet worden herberekend.' }),
    );
    expect(component.recalculating()).toBe(false);
  });

  it('formats highlights, minutes and unset dates', () => {
    create();
    expect(component.highlights(stats).map((h) => h.value)).toEqual([4, '12.50', '3 dagen', 1]);
    expect(component.minutes(2147483647)).toBe('Geen gegevens');
    expect(component.minutes(5)).toBe('5 min.');
    expect(component.isDateUnset()).toBe(true);
    expect(component.isDateUnset('0001-01-01T00:00:00')).toBe(true);
    expect(component.isDateUnset('2026-01-01T00:00:00Z')).toBe(false);
  });
});
