import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AchievementService } from './achievement.service';
import { Achievement, PaginationObject } from '../../../core/models/achievement.model';

describe('AchievementService', () => {
  let service: AchievementService;
  let httpMock: HttpTestingController;

  const mockAchievements: PaginationObject<Achievement> = {
    items: [
      { id: '1', name: 'First Achievement', description: 'Test', image: { uri: 'test.png' } as any, order: 1 } as any
    ],
    totalCount: 1
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AchievementService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(AchievementService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch achievements (getAchievements)', () => {
    service.getAchievements(0, 10).subscribe((res) => {
      expect(res.items.length).toBe(1);
      expect(res.totalCount).toBe(1);
    });

    const req = httpMock.expectOne((req) => req.url === '/api/v1/achievement');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('skip')).toBe('0');
    expect(req.request.params.get('take')).toBe('10');
    req.flush(mockAchievements);
  });

  it('should fetch achievement by id (getAchievementById)', () => {
    const mockAchievement: Achievement = mockAchievements.items[0];
    service.getAchievementById('1').subscribe((res) => {
      expect(res.name).toBe('First Achievement');
    });

    const req = httpMock.expectOne('/api/v1/achievement/1');
    expect(req.request.method).toBe('GET');
    req.flush(mockAchievement);
  });

  it('should delete achievement (deleteAchievement)', () => {
    service.deleteAchievement('1').subscribe();
    const req = httpMock.expectOne('/api/v1/achievement/1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
