import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController, TestRequest } from '@angular/common/http/testing';
import { vi } from 'vitest';
import { AchievementService } from './achievement.service';

describe('AchievementService uploads', () => {
  let service: AchievementService;
  let httpMock: HttpTestingController;
  const baseUrl = '/api/v1/achievement';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AchievementService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  const waitForRequest = (method: string) =>
    vi.waitFor((): TestRequest => httpMock.expectOne((r) => r.method === method && r.url === baseUrl));

  it('creates an achievement with its image as base64', async () => {
    const image = new File(['img'], 'badge.png', { type: 'image/png' });
    service
      .createAchievement({
        name: 'Nieuw',
        description: 'd',
        autoAchieve: true,
        action: 'OrdersCount',
        comparisonOperator: 'GreaterThan',
        comparisonValue: 3,
        image,
        tier: 'Bronze',
      })
      .subscribe();

    const req = await waitForRequest('POST');
    expect(req.request.body).toEqual(
      expect.objectContaining({
        name: 'Nieuw',
        comparisonValue: 3,
        image: { fileName: 'badge.png', contentType: 'image/png', base64Content: btoa('img') },
      }),
    );
    req.flush(null);
  });

  it('falls back to a default file name and content type', async () => {
    const image = new File(['img'], '');
    service
      .createAchievement({
        name: 'Nieuw',
        description: 'd',
        autoAchieve: false,
        action: 'None',
        comparisonOperator: 'None',
        comparisonValue: 0,
        image,
        tier: 'Bronze',
      })
      .subscribe();

    const req = await waitForRequest('POST');
    expect(req.request.body.image).toEqual(
      expect.objectContaining({ fileName: 'achievement-image', contentType: 'application/octet-stream' }),
    );
    req.flush(null);
  });

  it('updates an achievement with and without a new image', async () => {
    const dto = { id: 'a1', name: 'A' } as any;

    service.updateAchievement(dto).subscribe();
    const plain = httpMock.expectOne((r) => r.method === 'PUT' && r.url === baseUrl);
    expect(plain.request.body).toEqual(dto);
    plain.flush(null);

    service.updateAchievement(dto, new File(['new'], 'new.png', { type: 'image/png' })).subscribe();
    const withImage = await waitForRequest('PUT');
    expect(withImage.request.body.newImage).toEqual({
      fileName: 'new.png',
      contentType: 'image/png',
      base64Content: btoa('new'),
    });
    withImage.flush(null);

    service.updateAchievement(dto, new File(['new'], '')).subscribe();
    const defaults = await waitForRequest('PUT');
    expect(defaults.request.body.newImage).toEqual(
      expect.objectContaining({ fileName: 'achievement-image', contentType: 'application/octet-stream' }),
    );
    defaults.flush(null);
  });

  it('fetches the rarity of an achievement', () => {
    service.getRarity('a1').subscribe((res) => expect(res.count).toBe(4));
    httpMock.expectOne(`${baseUrl}/rarity/a1`).flush({ count: 4 });
  });
});
