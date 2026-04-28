import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { QuoteService } from './quote.service';
import { QuoteDto } from '../../../core/models/quote.model';

describe('QuoteService', () => {
  let service: QuoteService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        QuoteService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(QuoteService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch quotes', () => {
    const mockResponse = { items: [], totalCount: 0 };
    service.getQuotes(0, 10).subscribe(res => {
      expect(res).toEqual(mockResponse);
    });

    const req = httpMock.expectOne(r => r.url === '/api/v1/Quote' && r.params.get('skip') === '0');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should toggle vote', () => {
    service.toggleVote('quote-1').subscribe(res => {
      expect(res).toBe(true);
    });

    const req = httpMock.expectOne('/api/v1/Quote/quote-1/vote');
    expect(req.request.method).toBe('POST');
    req.flush(true);
  });

  it('should fetch single quote', () => {
    const mockQuote = { id: 'quote-1' } as QuoteDto;
    service.getQuote('quote-1').subscribe(res => {
      expect(res).toEqual(mockQuote);
    });

    const req = httpMock.expectOne('/api/v1/Quote/quote-1');
    expect(req.request.method).toBe('GET');
    req.flush(mockQuote);
  });
});
