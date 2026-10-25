import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';
import { vi } from 'vitest';
import AchievementsComponent from './achievements.component';
import { AchievementService } from '../../achievements/services/achievement.service';
import { ApplicationUserService } from '../../users/services/application-user.service';

describe('Admin AchievementsComponent', () => {
  let component: AchievementsComponent;
  let achievementService: any;
  let userService: { getUsers: ReturnType<typeof vi.fn> };
  let messageService: { add: ReturnType<typeof vi.fn> };
  let confirmationService: { confirm: ReturnType<typeof vi.fn> };

  const achievements = [
    { id: 'a1', name: 'Eerste', description: 'd', image: { uri: '/img/a1' }, autoAchieve: false, action: 'None', comparisonOperator: 'None', comparisonValue: 0, tier: 'Bronze' },
    { id: 'a2', name: 'Tweede', description: 'd', image: null, autoAchieve: true, action: 'QuoteCount', comparisonOperator: 'GreaterThan', comparisonValue: 3, tier: 'Gold', parentAchievementId: 'a1' },
  ] as any[];
  const user = { id: 'u1', fullName: 'Jan Jansen' } as any;
  const severities = () => messageService.add.mock.calls.map((c) => c[0].severity);

  beforeEach(() => {
    achievementService = {
      getAchievements: vi.fn(() => of({ items: achievements, totalCount: 2 })),
      createAchievement: vi.fn(() => of(void 0)),
      updateAchievement: vi.fn(() => of(void 0)),
      deleteAchievement: vi.fn(() => of(void 0)),
      awardAchievementToAllUsers: vi.fn(() => of(7)),
      getAchievementEntries: vi.fn(() => of([{ id: 'entry1', achievementId: 'a1', achievementName: 'Eerste', imageUrl: '/img/a1' }])),
      awardAchievementToUser: vi.fn(() => of(void 0)),
      deleteAchievementEntries: vi.fn(() => of(void 0)),
    };
    userService = { getUsers: vi.fn(() => of({ items: [user] })) };
    messageService = { add: vi.fn() };
    confirmationService = { confirm: vi.fn((c: any) => c.accept()) };

    TestBed.configureTestingModule({
      imports: [AchievementsComponent],
      providers: [
        { provide: AchievementService, useValue: achievementService },
        { provide: ApplicationUserService, useValue: userService },
        { provide: MessageService, useValue: messageService },
      ],
    });
    TestBed.overrideComponent(AchievementsComponent, {
      set: { template: '', providers: [{ provide: ConfirmationService, useValue: confirmationService }] },
    });
    const fixture = TestBed.createComponent(AchievementsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads achievements for the table, pick list and parent options', () => {
    expect(component.achievements()).toEqual(achievements);
    expect(component.totalRecords()).toBe(2);
    expect(component.allAchievementsForPick()).toEqual([
      { id: 'a1', name: 'Eerste', imageUri: '/img/a1' },
      { id: 'a2', name: 'Tweede', imageUri: undefined },
    ]);
    expect(component.parentOptions()).toEqual([{ name: 'Eerste', id: 'a1' }, { name: 'Tweede', id: 'a2' }]);
  });

  it('reports a failed load', () => {
    achievementService.getAchievements.mockReturnValue(throwError(() => new Error('x')));
    component.loadAchievements();
    expect(component.loading()).toBe(false);
    expect(severities()).toEqual(['error']);
  });

  it('requires an image for a new achievement', () => {
    component.openCreateDialog();
    expect(component.dialogVisible()).toBe(true);
    component.saveAchievement();
    expect(achievementService.createAchievement).not.toHaveBeenCalled();

    component.achForm.patchValue({ name: 'Nieuw', description: 'd' });
    component.saveAchievement();
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'Afbeelding is verplicht' }));
    expect(component.saving()).toBe(false);
  });

  it('creates an achievement with the selected image', () => {
    const file = new File(['x'], 'a.png');
    component.openCreateDialog();
    component.onFileSelected({ target: { files: [] } });
    component.onFileSelected({ target: { files: [file] } });
    component.achForm.patchValue({ name: 'Nieuw', description: 'd' });
    component.saveAchievement();

    expect(achievementService.createAchievement).toHaveBeenCalledWith(
      expect.objectContaining({ name: 'Nieuw', image: file, parentAchievementId: undefined }),
    );
    expect(component.dialogVisible()).toBe(false);
    expect(severities()).toEqual(['success']);

    achievementService.createAchievement.mockReturnValue(throwError(() => new Error('x')));
    component.saveAchievement();
    expect(messageService.add).toHaveBeenLastCalledWith(expect.objectContaining({ detail: 'Toevoegen is mislukt' }));
  });

  it('edits an achievement', () => {
    component.openEditDialog(achievements[1]);
    expect(component.isEdit()).toBe(true);
    expect(component.currentImageUri()).toBeNull();
    expect(component.achForm.getRawValue().parentAchievementId).toBe('a1');

    component.saveAchievement();
    expect(achievementService.updateAchievement).toHaveBeenCalledWith(
      expect.objectContaining({ id: 'a2', name: 'Tweede', comparisonValue: 3 }),
      undefined,
    );
    expect(severities()).toEqual(['success']);

    achievementService.updateAchievement.mockReturnValue(throwError(() => new Error('x')));
    component.openEditDialog(achievements[0]);
    component.saveAchievement();
    expect(messageService.add).toHaveBeenLastCalledWith(expect.objectContaining({ detail: 'Bewerken is mislukt' }));
  });

  it('deletes an achievement after confirmation', () => {
    component.confirmDelete(achievements[0]);
    expect(achievementService.deleteAchievement).toHaveBeenCalledWith('a1');
    expect(component.deletingId()).toBeNull();

    achievementService.deleteAchievement.mockReturnValue(throwError(() => new Error('x')));
    component.deleteAchievement('a1');
    expect(severities()).toEqual(['success', 'error']);
  });

  it('awards an achievement to all users', () => {
    component.confirmAwardAll(achievements[0]);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'Toegekend aan 7 gebruikers' }));

    achievementService.awardAchievementToAllUsers.mockReturnValue(throwError(() => new Error('x')));
    component.confirmAwardAll(achievements[0]);
    expect(messageService.add).toHaveBeenLastCalledWith(expect.objectContaining({ detail: 'Toekennen is mislukt' }));
  });

  it('manages the achievements of a user', () => {
    component.openUserDialog();
    component.openUserDialog();
    expect(userService.getUsers).toHaveBeenCalledTimes(1);
    expect(component.users()).toEqual([user]);

    component.onUserSelect(user);
    expect(component.userAchievements()).toEqual([{ id: 'a1', name: 'Eerste', imageUri: '/img/a1', entryId: 'entry1' }]);
    expect(component.availableAchievements().map((a) => a.id)).toEqual(['a2']);

    component.loadAchievements();
    expect(achievementService.getAchievementEntries).toHaveBeenCalledTimes(2);

    component.onUserSelect(null);
    expect(component.userAchievements()).toEqual([]);
    expect(component.availableAchievements()).toEqual([]);
  });

  it('awards and removes achievements by moving them in the pick list', () => {
    component.onMoveToTarget({ items: [{ id: 'a2', name: 'Tweede' }] });
    component.onMoveToSource({ items: [{ id: 'a1', entryId: 'entry1' }] });
    expect(achievementService.awardAchievementToUser).not.toHaveBeenCalled();

    component.onUserSelect(user);
    component.onMoveToTarget({ items: [{ id: 'a2', name: 'Tweede' }] });
    expect(achievementService.awardAchievementToUser).toHaveBeenCalledWith('u1', 'a2');

    component.onMoveToSource({ items: [{ id: 'a2' }] });
    expect(achievementService.deleteAchievementEntries).not.toHaveBeenCalled();
    component.onMoveToSource({ items: [{ id: 'a1', entryId: 'entry1' }, { id: 'a2' }] });
    expect(achievementService.deleteAchievementEntries).toHaveBeenCalledWith(['entry1']);
    expect(severities()).toEqual(['success', 'info']);

    achievementService.awardAchievementToUser.mockReturnValue(throwError(() => new Error('x')));
    achievementService.deleteAchievementEntries.mockReturnValue(throwError(() => new Error('x')));
    component.onMoveToTarget({ items: [{ id: 'a2', name: 'Tweede' }] });
    component.onMoveToSource({ items: [{ id: 'a1', entryId: 'entry1' }] });
    expect(severities()).toEqual(['success', 'info', 'error', 'error']);
  });
});
