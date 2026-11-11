import { TestBed, ComponentFixture } from '@angular/core/testing';
import { WhatsNewModalComponent } from './whats-new-modal.component';
import { WhatsNewService } from '../../../features/admin/services/whats-new.service';
import { LanguageService } from '../../../core/services/language.service';
import { signal } from '@angular/core';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { vi } from 'vitest';

describe('WhatsNewModalComponent', () => {
  let component: WhatsNewModalComponent;
  let fixture: ComponentFixture<WhatsNewModalComponent>;
  let mockWhatsNewService: any;
  let mockLanguageService: any;

  beforeEach(async () => {
    TestBed.resetTestingModule();
    mockWhatsNewService = {
      isVisible: signal(false),
      content: signal('Test Content'),
      markAsSeen: vi.fn()
    };

    mockLanguageService = {
      t: signal({
        whats_new_modal_title: 'Title',
        awesome: 'Awesome'
      })
    };

    await TestBed.configureTestingModule({
      imports: [WhatsNewModalComponent, NoopAnimationsModule],
      providers: [
        { provide: WhatsNewService, useValue: mockWhatsNewService },
        { provide: LanguageService, useValue: mockLanguageService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(WhatsNewModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should not show modal by default', () => {
    const dialog = fixture.nativeElement.querySelector('p-dialog');
    // PrimeNG Dialog might still be in DOM but hidden
    expect(component.isVisible).toBe(false);
  });

  it('should show content when visible', () => {
    mockWhatsNewService.isVisible.set(true);
    fixture.detectChanges();
    expect(component.isVisible).toBe(true);
    const content = fixture.nativeElement.querySelector('.rich-text-content');
    expect(content.innerHTML).toContain('Test Content');
  });

  it('should call markAsSeen when closed', () => {
    component.onClose();
    expect(mockWhatsNewService.markAsSeen).toHaveBeenCalled();
  });

  it('should match snapshot when visible', () => {
    mockWhatsNewService.isVisible.set(true);
    fixture.detectChanges();
    expect(withoutAnimationState(fixture.nativeElement)).toMatchSnapshot();
  });
});

/**
 * A copy without PrimeNG's enter/leave animation state (`*-enter-*` / `*-leave-*` classes, `data-enter-*` /
 * `data-leave-*` attributes). Whether those are set depends on whether an animation frame ran before the
 * snapshot, which varies under load (the full suite in parallel), so they made this snapshot flaky.
 */
function withoutAnimationState(element: HTMLElement): HTMLElement {
  const copy = element.cloneNode(true) as HTMLElement;
  for (const el of [copy, ...Array.from(copy.querySelectorAll('*'))]) {
    for (const cls of Array.from(el.classList)) {
      if (/-(enter|leave)(-|$)/.test(cls)) {
        el.classList.remove(cls);
      }
    }
    for (const attr of Array.from(el.attributes)) {
      if (/^data-(enter|leave)-/.test(attr.name)) {
        el.removeAttribute(attr.name);
      }
    }
  }
  return copy;
}
