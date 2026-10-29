import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { QuoteCardComponent } from './quote-card.component';
import { QuoteService } from '../../services/quote.service';

describe('QuoteCardComponent', () => {
  let component: QuoteCardComponent;
  let router: { navigate: ReturnType<typeof vi.fn> };
  let quoteService: { toggleVote: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    router = { navigate: vi.fn() };
    quoteService = { toggleVote: vi.fn(() => of(void 0)) };
    TestBed.configureTestingModule({
      imports: [QuoteCardComponent],
      providers: [
        { provide: Router, useValue: router },
        { provide: QuoteService, useValue: quoteService },
      ],
    });
    TestBed.overrideComponent(QuoteCardComponent, { set: { template: '' } });
    component = TestBed.createComponent(QuoteCardComponent).componentInstance;
    component.quote = { id: 'q1' } as any;
  });

  it('opens the quote detail page', () => {
    component.navigateToDetail(new MouseEvent('click'));
    expect(router.navigate).toHaveBeenCalledWith(['/quotes', 'q1']);
  });

  it('toggles a vote without opening the quote', () => {
    const voted = vi.fn();
    component.onVote.subscribe(voted);
    const event = new MouseEvent('click');
    const stop = vi.spyOn(event, 'stopPropagation');

    component.toggleVote(event);

    expect(stop).toHaveBeenCalled();
    expect(quoteService.toggleVote).toHaveBeenCalledWith('q1');
    expect(voted).toHaveBeenCalledWith(component.quote);
  });
});
