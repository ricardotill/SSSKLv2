import { TestBed } from '@angular/core/testing';
import { AchievementPopupService } from './achievement-popup.service';
import { AchievementService } from './achievement.service';
import { of } from 'rxjs';
import { vi } from 'vitest';

describe('AchievementPopupService', () => {
  let service: AchievementPopupService;
  let achievementServiceMock: any;

  beforeEach(() => {
    achievementServiceMock = {
      getUnseenAchievementEntries: vi.fn().mockReturnValue(of([]))
    };

    TestBed.configureTestingModule({
      providers: [
        AchievementPopupService,
        { provide: AchievementService, useValue: achievementServiceMock }
      ]
    });
    service = TestBed.inject(AchievementPopupService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize with null unseenEntries', () => {
    expect(service.unseenEntries()).toBeNull();
  });

  it('should set unseenEntries when checkUnseenAchievements finds entries', () => {
    const mockEntries = [{ id: '1', achievementName: 'Test' }] as any;
    achievementServiceMock.getUnseenAchievementEntries.mockReturnValue(of(mockEntries));
    
    service.checkUnseenAchievements();
    expect(service.unseenEntries()).toEqual(mockEntries);
  });

  it('should clear unseenEntries', () => {
    service.unseenEntries.set([{ id: '1' }] as any);
    service.clear();
    expect(service.unseenEntries()).toBeNull();
  });
});
