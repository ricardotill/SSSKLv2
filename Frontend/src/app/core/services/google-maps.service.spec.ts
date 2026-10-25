import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { GoogleMapsService } from './google-maps.service';
import { GlobalSettingsService } from '../../features/admin/services/global-settings.service';

describe('GoogleMapsService', () => {
  let service: GoogleMapsService;
  let getSetting: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getSetting = vi.fn(() => of({ value: '' }));
    TestBed.configureTestingModule({
      providers: [{ provide: GlobalSettingsService, useValue: { getSetting } }],
    });
    service = TestBed.inject(GoogleMapsService);
  });

  it('does not load without an API key and caches the result', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    await expect(firstValueFrom(service.load())).resolves.toBe(false);
    await expect(firstValueFrom(service.load())).resolves.toBe(false);
    expect(getSetting).toHaveBeenCalledTimes(1);
    expect(warn).toHaveBeenCalled();
  });

  it('does not load when the API key cannot be fetched', async () => {
    const error = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    getSetting.mockReturnValue(throwError(() => new Error('x')));
    await expect(firstValueFrom(service.load())).resolves.toBe(false);
    expect(error).toHaveBeenCalled();
  });

  it('returns the map id when configured', async () => {
    getSetting.mockReturnValue(of({ value: 'map-1' }));
    await expect(firstValueFrom(service.getMapId())).resolves.toBe('map-1');

    getSetting.mockReturnValue(of({ value: '' }));
    await expect(firstValueFrom(service.getMapId())).resolves.toBeUndefined();

    getSetting.mockReturnValue(throwError(() => new Error('x')));
    await expect(firstValueFrom(service.getMapId())).resolves.toBeUndefined();
  });

  it('reports whether an API key is configured', async () => {
    getSetting.mockReturnValue(of({ value: 'key' }));
    await expect(firstValueFrom(service.hasApiKey())).resolves.toBe(true);

    getSetting.mockReturnValue(throwError(() => new Error('x')));
    await expect(firstValueFrom(service.hasApiKey())).resolves.toBe(false);
  });
});
