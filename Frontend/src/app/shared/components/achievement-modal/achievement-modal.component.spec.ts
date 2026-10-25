import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { vi } from 'vitest';
import { AchievementModalComponent } from './achievement-modal.component';
import { AchievementPopupService } from '../../../features/achievements/services/achievement-popup.service';

describe('AchievementModalComponent', () => {
  let component: AchievementModalComponent;
  let fixture: any;
  let unseenEntries: ReturnType<typeof signal<any[]>>;
  let popupService: { unseenEntries: typeof unseenEntries; clear: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    vi.useFakeTimers();
    unseenEntries = signal<any[]>([]);
    popupService = { unseenEntries, clear: vi.fn() };
    vi.stubGlobal('requestAnimationFrame', vi.fn());
    HTMLCanvasElement.prototype.getContext = vi.fn(() => null) as any;

    TestBed.configureTestingModule({
      imports: [AchievementModalComponent],
      providers: [{ provide: AchievementPopupService, useValue: popupService }],
    });
    TestBed.overrideComponent(AchievementModalComponent, { set: { template: '' } });
    fixture = TestBed.createComponent(AchievementModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  it('stays hidden without unseen achievements', () => {
    expect(component.isVisible).toBe(false);
    expect(component.entries).toEqual([]);
  });

  it('shows unseen achievements with confetti', () => {
    unseenEntries.set([{ id: 'e1' }]);
    fixture.detectChanges();

    expect(component.isVisible).toBe(true);
    expect(component.entries).toEqual([{ id: 'e1' }]);
    expect(requestAnimationFrame).toHaveBeenCalled();
  });

  it('clears the popup when closed or hidden', () => {
    component.close();
    expect(component.isVisible).toBe(false);
    expect(popupService.clear).not.toHaveBeenCalled();
    vi.advanceTimersByTime(300);
    expect(popupService.clear).toHaveBeenCalledTimes(1);

    component.onHide();
    expect(popupService.clear).toHaveBeenCalledTimes(2);
  });
});
