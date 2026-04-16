import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ReactionService } from './reaction.service';
import { ReactionDto } from '../models/reaction.model';

describe('ReactionService', () => {
  let service: ReactionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ReactionService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(ReactionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call toggle endpoint', () => {
    service.toggleReaction('123', 'Event', '👍').subscribe();

    const req = httpMock.expectOne('/api/v1/Reactions/toggle');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ targetId: '123', targetType: 'Event', content: '👍' });
    req.flush({});
  });

  it('should fetch reactions for target', () => {
    const mockReactions: ReactionDto[] = [
      { id: '1', userId: 'u1', userName: 'User', content: '👍', targetId: '123', targetType: 'Event', createdOn: new Date() }
    ];

    service.getReactions('123', 'Event').subscribe(res => {
      expect(res).toEqual(mockReactions);
    });

    const req = httpMock.expectOne('/api/v1/Reactions/123/Event');
    expect(req.request.method).toBe('GET');
    req.flush(mockReactions);
  });

  it('should fetch timeline', () => {
    service.getTimeline(0, 10).subscribe();

    const req = httpMock.expectOne(req => req.url === '/api/v1/Reactions/timeline' && req.params.get('skip') === '0');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
