import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { LeaderboardService } from './leaderboard.service';

describe('LeaderboardService', () => {
  let service: LeaderboardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        LeaderboardService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(LeaderboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch leaderboard', () => {
    service.getLeaderboard('1').subscribe();
    const req = httpMock.expectOne('/api/v1/leaderboard/1');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('should fetch monthly leaderboard', () => {
    service.getMonthlyLeaderboard('1').subscribe();
    const req = httpMock.expectOne('/api/v1/leaderboard/monthly/1');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
