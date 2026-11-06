import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { vi } from 'vitest';
import { RolesComponent } from './roles.component';
import { RoleService } from '../services/role.service';
import { LanguageService } from '../../../core/services/language.service';

describe('RolesComponent (rendered, mobile layout)', () => {
  const row: any = { id: 'r1', name: 'Penningmeester' };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RolesComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: RoleService, useValue: { getAdminRoles: vi.fn(() => of([row])) } },
      ],
    }).compileComponents();
  });

  it('marks the table as stackable and labels every body cell with its column header', async () => {
    const fixture = TestBed.createComponent(RolesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const el: HTMLElement = fixture.nativeElement;
    const ls = TestBed.inject(LanguageService);
    const t = ls.t() as Record<string, string>;

    expect(el.querySelector('p-table.responsive-stack-table')).not.toBeNull();
    expect(el.textContent).toContain('Penningmeester');
    const labels = Array.from(el.querySelectorAll('tbody tr:first-child td')).map((td) => td.getAttribute('data-label'));
    expect(labels).toEqual(['roles_name', 'roles_actions'].map((k) => t[k]));
  });
});
