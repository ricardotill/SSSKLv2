import { TestBed, ComponentFixture } from '@angular/core/testing';
import { LocationSelectorComponent } from './location-selector.component';
import { GoogleMapsService } from '../../../core/services/google-maps.service';
import { ThemeService } from '../../../core/services/theme.service';
import { of } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('LocationSelectorComponent', () => {
  let component: LocationSelectorComponent;
  let fixture: ComponentFixture<LocationSelectorComponent>;
  let googleMapsServiceMock: any;
  let themeServiceMock: any;

  beforeEach(async () => {
    googleMapsServiceMock = {
      load: vi.fn().mockReturnValue(of(true)),
      getMapId: vi.fn().mockReturnValue(of('test-map-id'))
    };

    themeServiceMock = {
      isDark: vi.fn().mockReturnValue(false)
    };

    // Mock window.google object
    (window as any).google = {
      maps: {
        Map: vi.fn().mockImplementation(function() {
          return {
            addListener: vi.fn(),
            setCenter: vi.fn(),
            setZoom: vi.fn()
          };
        }),
        ColorScheme: {
          DARK: 'DARK',
          LIGHT: 'LIGHT'
        },
        importLibrary: vi.fn().mockImplementation(async (lib) => {
          if (lib === 'marker') {
            return {
              AdvancedMarkerElement: vi.fn().mockImplementation(function() {
                return {
                  addListener: vi.fn(),
                  position: { lat: 0, lng: 0 }
                };
              })
            };
          }
          return {};
        })
      }
    };

    await TestBed.configureTestingModule({
      imports: [LocationSelectorComponent],
      providers: [
        { provide: GoogleMapsService, useValue: googleMapsServiceMock },
        { provide: ThemeService, useValue: themeServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LocationSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('sanitizeCoordinate', () => {
    it('should keep valid coordinates unchanged', () => {
      const result = (component as any).sanitizeCoordinate(52.3676);
      expect(result).toBe(52.3676);
    });

    it('should heal mangled coordinates by scaling down', () => {
      // Example of typical mangled coordinate: 536353316 should become 53.6353316
      const mangled = 536353316;
      const result = (component as any).sanitizeCoordinate(mangled);
      // We expect it to be divided by 10 until it hits the range (-180, 180)
      // 536353316 -> 53635331.6 -> 5363533.16 -> 536353.316 -> 53635.3316 -> 5363.53316 -> 536.353316 -> 53.6353316
      expect(result).toBeCloseTo(53.6353316, 5);
    });

    it('should handle extremely mangled coordinates', () => {
      const result = (component as any).sanitizeCoordinate(11400313600);
      expect(result).toBeLessThanOrEqual(180);
      expect(result).toBeGreaterThanOrEqual(-180);
    });

    it('should handle undefined and null', () => {
      expect((component as any).sanitizeCoordinate(undefined)).toBeUndefined();
      expect((component as any).sanitizeCoordinate(null as any)).toBeUndefined();
    });

    it('should handle 0', () => {
      expect((component as any).sanitizeCoordinate(0)).toBe(0);
    });
  });

  it('should emit null when cleared', () => {
    const emitSpy = vi.spyOn(component.locationChanged, 'emit');
    component.clearSelection();
    expect(emitSpy).toHaveBeenCalledWith(null);
  });
});
