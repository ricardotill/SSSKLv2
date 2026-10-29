import { TestBed, ComponentFixture } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import { LocationSelectorComponent } from './location-selector.component';
import { GoogleMapsService } from '../../../core/services/google-maps.service';
import { ThemeService } from '../../../core/services/theme.service';

describe('LocationSelectorComponent with the map', () => {
  let fixture: ComponentFixture<LocationSelectorComponent>;
  let component: LocationSelectorComponent;
  let mapsLoaded: boolean;
  let mapListeners: Record<string, (e: any) => void>;
  let markerListeners: Record<string, () => void>;
  let marker: any;
  let map: any;
  let geocode: ReturnType<typeof vi.fn>;
  let emitted: any[];

  async function create(inputs: Record<string, unknown> = {}) {
    await TestBed.configureTestingModule({
      imports: [LocationSelectorComponent],
      providers: [
        { provide: GoogleMapsService, useValue: { load: () => of(mapsLoaded), getMapId: () => of(undefined) } },
        { provide: ThemeService, useValue: { isDark: () => true } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LocationSelectorComponent);
    component = fixture.componentInstance;
    for (const [k, v] of Object.entries(inputs)) fixture.componentRef.setInput(k, v);
    emitted = [];
    component.locationChanged.subscribe((v) => emitted.push(v));
    fixture.detectChanges();
    fixture.detectChanges();
    await vi.advanceTimersByTimeAsync(50);
  }

  beforeEach(() => {
    vi.useFakeTimers();
    mapsLoaded = true;
    mapListeners = {};
    markerListeners = {};
    geocode = vi.fn(() => Promise.resolve({ results: [{ formatted_address: 'Dorpsstraat 1' }] }));
    vi.stubGlobal('google', {
      maps: {
        Map: vi.fn(function () {
          map = {
            addListener: vi.fn((name: string, cb: any) => (mapListeners[name] = cb)),
            setCenter: vi.fn(),
            setZoom: vi.fn(),
          };
          return map;
        }),
        Geocoder: vi.fn(function () {
          return { geocode };
        }),
        ColorScheme: { DARK: 'DARK', LIGHT: 'LIGHT' },
        importLibrary: vi.fn(async () => ({
          AdvancedMarkerElement: vi.fn(function (opts: any) {
            marker = {
              ...opts,
              addListener: vi.fn((name: string, cb: any) => (markerListeners[name] = cb)),
            };
            return marker;
          }),
        })),
      },
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.useRealTimers();
  });

  it('shows the manual fallback when Google Maps cannot be loaded', async () => {
    mapsLoaded = false;
    await create({ initialName: 'Clubhuis' });

    expect(component.loadingApi()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('Google Maps kon niet worden geladen');
    expect(component.manualLocationName).toBe('Clubhuis');

    component.manualLocationName = 'Bos';
    component.onManualLocationChange();
    expect(emitted).toEqual([{ name: 'Bos', lat: 0, lng: 0 }]);
  });

  it('creates the map with a marker at the initial location', async () => {
    await create({ initialLatitude: 52.1, initialLongitude: 5.1, initialName: 'Clubhuis' });

    expect(google.maps.Map).toHaveBeenCalledWith(
      expect.any(HTMLElement),
      expect.objectContaining({ center: { lat: 52.1, lng: 5.1 }, zoom: 15, colorScheme: 'DARK' }),
    );
    expect(marker).toEqual(expect.objectContaining({ title: 'Clubhuis', position: { lat: 52.1, lng: 5.1 } }));
    expect(component.selectedLocationName()).toBe('Clubhuis');
    expect(fixture.nativeElement.textContent).toContain('Clubhuis');
  });

  it('centres on the Netherlands without an initial location', async () => {
    await create();
    expect(google.maps.Map).toHaveBeenCalledWith(
      expect.any(HTMLElement),
      expect.objectContaining({ center: { lat: 52.0907, lng: 5.1214 }, zoom: 7 }),
    );
  });

  it('selects a clicked location using its reverse geocoded address', async () => {
    await create();

    mapListeners['click']({ latLng: { lat: () => 51.5, lng: () => 4.5 } });
    await vi.advanceTimersByTimeAsync(0);

    expect(geocode).toHaveBeenCalledWith({ location: { lat: 51.5, lng: 4.5 } });
    expect(emitted).toEqual([{ name: 'Dorpsstraat 1', lat: 51.5, lng: 4.5 }]);
    expect(component.selectedLocationName()).toBe('Dorpsstraat 1');
  });

  it('falls back to coordinates when reverse geocoding fails', async () => {
    geocode.mockImplementation(() => Promise.reject(new Error('ZERO_RESULTS')));
    await create();

    mapListeners['click']({ latLng: { lat: () => 51.5, lng: () => 4.5 } });
    await vi.advanceTimersByTimeAsync(0);

    expect(emitted).toEqual([{ name: '51.5000, 4.5000', lat: 51.5, lng: 4.5 }]);
  });

  it('ignores map clicks without a position', async () => {
    await create();
    mapListeners['click']({});
    expect(geocode).not.toHaveBeenCalled();
  });

  it('moves the marker and reselects after it is dragged', async () => {
    await create({ initialLatitude: 52.1, initialLongitude: 5.1, initialName: 'Clubhuis' });

    marker.position = { lat: () => 52.2, lng: () => 5.2 };
    markerListeners['dragend']();
    await vi.advanceTimersByTimeAsync(0);

    expect(geocode).toHaveBeenCalledWith({ location: { lat: 52.2, lng: 5.2 } });
    expect(marker.position).toEqual({ lat: 52.2, lng: 5.2 });
  });

  it('selects a place picked from autocomplete', async () => {
    await create();
    const place = {
      location: { lat: () => 52.3, lng: () => 4.9 },
      displayName: 'Amsterdam',
      fetchFields: vi.fn(async () => undefined),
    };

    await component.handlePlaceSelect({ placePrediction: { toPlace: async () => place } });

    expect(place.fetchFields).toHaveBeenCalled();
    expect(emitted).toEqual([{ name: 'Amsterdam', lat: 52.3, lng: 4.9 }]);
    expect(map.setCenter).toHaveBeenCalledWith({ lat: 52.3, lng: 4.9 });
    expect(map.setZoom).toHaveBeenCalledWith(17);
  });

  it('ignores autocomplete events without a usable place', async () => {
    await create();
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => undefined);
    const error = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    await component.handlePlaceSelect({});
    await component.handlePlaceSelect({ placePrediction: { toPlace: async () => ({ fetchFields: async () => undefined }) } });
    await component.handlePlaceSelect({ placePrediction: { toPlace: async () => { throw new Error('x'); } } });

    expect(emitted).toEqual([]);
    expect(warn).toHaveBeenCalled();
    expect(error).toHaveBeenCalled();
  });

  it('removes the marker when the selection is cleared', async () => {
    await create({ initialLatitude: 52.1, initialLongitude: 5.1, initialName: 'Clubhuis' });
    const placed = marker;

    fixture.nativeElement.querySelector('button.pi-times').click();

    expect(placed.map).toBeNull();
    expect(component.selectedLocationName()).toBeUndefined();
    expect(emitted).toEqual([null]);
    fixture.destroy();
  });
});
