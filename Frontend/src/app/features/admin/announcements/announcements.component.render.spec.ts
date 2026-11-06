import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import AnnouncementsComponent from './announcements.component';
import { AnnouncementService } from '../services/announcement.service';
import { LanguageService } from '../../../core/services/language.service';

describe('AnnouncementsComponent (rendered, mobile layout)', () => {
  const row: any = { id: 'a1', message: 'Opkomst', description: 'Zaterdag', order: 1, isScheduled: true, plannedFrom: '2026-01-01T10:00:00Z', plannedTill: null };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AnnouncementsComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: AnnouncementService, useValue: { getAnnouncements: vi.fn(() => of({ items: [row], totalCount: 1 })) } },
      ],
    }).compileComponents();
  });

  it('marks the table as stackable and labels every body cell with its column header', async () => {
    const fixture = TestBed.createComponent(AnnouncementsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const ls = TestBed.inject(LanguageService);
    const t = ls.t() as Record<string, string>;

    expect(el.querySelector('p-table.responsive-stack-table')).not.toBeNull();
    expect(el.textContent).toContain('Opkomst');
    const labels = Array.from(el.querySelectorAll('tbody tr:first-child td')).map((td) => td.getAttribute('data-label'));
    expect(labels).toEqual(['message', 'announcement_order', 'is_scheduled', 'planned_from', 'planned_till', 'actions'].map((k) => t[k]));
  });
});
