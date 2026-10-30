import { TestBed, ComponentFixture } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';
import { vi } from 'vitest';
import AchievementsComponent from './achievements.component';
import { AchievementService } from '../../achievements/services/achievement.service';
import { ApplicationUserService } from '../../users/services/application-user.service';
import { UrlService } from '../../../core/services/url.service';

describe('Admin AchievementsComponent (rendered)', () => {
  let fixture: ComponentFixture<AchievementsComponent>;
  let component: AchievementsComponent;
  let achievementService: any;
  let confirmationService: ConfirmationService;

  const achievements = [
    { id: 'a1', name: 'Eerste', description: 'Eerste beschrijving', image: { uri: '/img/a1' }, autoAchieve: false, action: 'None', comparisonOperator: 'None', comparisonValue: 0, tier: 'Bronze' },
    { id: 'a2', name: 'Tweede', description: 'd', image: null, autoAchieve: true, action: 'QuoteCount', comparisonOperator: 'GreaterThan', comparisonValue: 3, tier: 'Gold', parentAchievementId: 'a1' },
    { id: 'a3', name: 'Derde', description: 'd', image: null, autoAchieve: false, action: 'None', comparisonOperator: 'None', comparisonValue: 0, tier: 'Silver' },
    { id: 'a4', name: 'Vierde', description: 'd', image: null, autoAchieve: false, action: 'None', comparisonOperator: 'None', comparisonValue: 0, tier: 'Platinum' },
  ] as any[];
  const user = { id: 'u1', fullName: 'Jan Jansen' } as any;

  const el = () => fixture.nativeElement as HTMLElement;
  const click = (label: string) => {
    const button = el().querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);
    button!.click();
    fixture.detectChanges();
  };

  beforeEach(async () => {
    achievementService = {
      getAchievements: vi.fn(() => of({ items: achievements, totalCount: achievements.length })),
      createAchievement: vi.fn(() => of(void 0)),
      updateAchievement: vi.fn(() => of(void 0)),
      deleteAchievement: vi.fn(() => of(void 0)),
      awardAchievementToAllUsers: vi.fn(() => of(7)),
      getAchievementEntries: vi.fn(() => of([{ id: 'entry1', achievementId: 'a1', achievementName: 'Eerste', imageUrl: '/img/a1' }])),
      awardAchievementToUser: vi.fn(() => of(void 0)),
      deleteAchievementEntries: vi.fn(() => of(void 0)),
    };

    await TestBed.configureTestingModule({
      imports: [AchievementsComponent, NoopAnimationsModule],
      providers: [
        { provide: AchievementService, useValue: achievementService },
        { provide: ApplicationUserService, useValue: { getUsers: vi.fn(() => of({ items: [user] })) } },
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: UrlService, useValue: { resolveApiUrl: (u: string | null | undefined) => (u ? `https://api${u}` : undefined) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AchievementsComponent);
    component = fixture.componentInstance;
    confirmationService = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmationService, 'confirm').mockImplementation((c: any) => {
      c.accept();
      return confirmationService;
    });
    fixture.detectChanges();
  });

  it('renders a row per achievement with its tier and condition', () => {
    const rows = el().querySelectorAll('tbody tr');
    expect(rows).toHaveLength(4);
    expect(el().textContent).toContain('Eerste beschrijving');
    expect(el().textContent).toContain('QuoteCount GreaterThan 3');
    expect(el().textContent).toContain('Handmatig');
    expect(el().querySelector('tbody img')?.getAttribute('src')).toBe('https://api/img/a1');
  });

  it('shows the empty message without achievements', () => {
    component.achievements.set([]);
    fixture.detectChanges();
    expect(el().textContent).toContain('Geen achievements gevonden');
  });

  it('runs the row actions', () => {
    click('Aan Iedereen Toekennen');
    expect(achievementService.awardAchievementToAllUsers).toHaveBeenCalledWith('a1');

    click('Verwijderen');
    expect(achievementService.deleteAchievement).toHaveBeenCalledWith('a1');

    click('Bewerken');
    expect(component.dialogVisible()).toBe(true);
    expect(component.isEdit()).toBe(true);
    expect(document.body.textContent).toContain('Huidige afbeelding:');
  });

  it('renders the create dialog with the auto-achieve fields', () => {
    component.openCreateDialog();
    component.achForm.patchValue({ autoAchieve: true });
    fixture.detectChanges();

    expect(document.body.textContent).toContain('Afbeelding (.png, .jpg)');
    expect(document.querySelectorAll('p-select').length).toBeGreaterThan(3);

    const input = document.querySelector<HTMLInputElement>('input#image')!;
    input.dispatchEvent(new Event('change'));
  });

  it('renders the user pick list once a user is selected', () => {
    component.openUserDialog();
    fixture.detectChanges();
    component.selectedUser.set(user);
    component.onUserSelect(user);
    fixture.detectChanges();

    expect(achievementService.getAchievementEntries).toHaveBeenCalled();
    expect(document.querySelector('p-pick-list, p-picklist')).toBeTruthy();
    expect(document.body.textContent).toContain('Achievements van Gebruiker');
  });
});
