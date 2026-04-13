import { Component, input, output, effect, ElementRef, viewChild, AfterViewInit, inject, signal, OnDestroy, CUSTOM_ELEMENTS_SCHEMA, HostListener } from '@angular/core';
// Triggering recompile to ensure Google Maps replacement is picked up
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';
import { GoogleMapsService } from '../../../core/services/google-maps.service';
import { ThemeService } from '../../../core/services/theme.service';
import { take } from 'rxjs';

export interface LocationResult {
  name: string;
  lat: number;
  lng: number;
}

@Component({
  selector: 'app-location-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, InputTextModule, ButtonModule, ProgressSpinnerModule, MessageModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  template: `
    <div class="flex flex-col gap-3">
      @if (loadingApi()) {
        <div class="flex items-center gap-2 p-4 bg-surface-50 dark:bg-surface-800 rounded-lg border border-dashed border-surface-200 dark:border-surface-700">
          <p-progress-spinner strokeWidth="4" styleClass="w-6 h-6"></p-progress-spinner>
          <span class="text-sm text-surface-500 font-bold">Google Maps wordt geladen...</span>
        </div>
      } @else if (!apiLoaded()) {
        <!-- Fallback UI -->
        <div class="flex flex-col gap-3 p-4 bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-900/30 rounded-lg">
          <div class="flex items-center gap-2 text-orange-700 dark:text-orange-400 font-medium text-sm">
            <i class="pi pi-exclamation-triangle"></i>
            <span>Google Maps kon niet worden geladen (ontbrekende API-sleutel). Voer de locatie handmatig in.</span>
          </div>
          <div class="flex gap-2">
            <input 
              pInputText 
              [(ngModel)]="manualLocationName" 
              (ngModelChange)="onManualLocationChange()" 
              [placeholder]="'Voer locatienaam in...'" 
              class="w-full"
            />
          </div>
        </div>
      } @else {
        <!-- Google Maps UI -->
        <div class="flex flex-col gap-3" (keydown.enter)="$event.preventDefault(); $event.stopPropagation()">
          <div class="relative flex-grow gmp-autocomplete-container">
            <gmp-place-autocomplete 
              #autocompleteElement 
              class="w-full"
              (gmp-select)="handlePlaceSelect($event)"
            ></gmp-place-autocomplete>
          </div>

          <div class="relative w-full h-[350px] rounded-lg overflow-hidden border border-surface-200 dark:border-surface-700 shadow-sm">
            <div #mapContainer class="w-full h-full z-0"></div>
          </div>

          @if (selectedLocationName()) {
            <div class="flex items-center gap-2 p-2 bg-primary-50 dark:bg-primary-900/20 text-primary-700 dark:text-primary-300 rounded-md text-sm font-medium border border-primary-100 dark:border-primary-900/30">
              <i class="pi pi-map-marker"></i>
              <span class="truncate">{{ selectedLocationName() }}</span>
              <button type="button" class="ml-auto pi pi-times hover:text-primary-900" (click)="clearSelection()"></button>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
    .gmp-autocomplete-container {
      --gmpx-border-radius: 8px;
      --gmpx-font-family: inherit;
    }
    gmp-place-autocomplete::part(input) {
      width: 100%;
      padding: 0.75rem 1rem;
      border: 1px solid var(--p-surface-200);
      border-radius: 8px;
      font-size: 1rem;
      background: var(--p-surface-0);
      color: var(--p-surface-900);
    }
    .dark gmp-place-autocomplete::part(input) {
      background: var(--p-surface-800);
      border-color: var(--p-surface-700);
      color: var(--p-surface-0);
    }
  `]
})
export class LocationSelectorComponent implements AfterViewInit, OnDestroy {
  private readonly googleMapsService = inject(GoogleMapsService);
  private readonly themeService = inject(ThemeService);

  // Inputs
  initialLatitude = input<number | undefined>();
  initialLongitude = input<number | undefined>();
  initialName = input<string | undefined>();

  // Outputs
  locationChanged = output<LocationResult | null>();

  // Template refs
  mapContainer = viewChild<ElementRef>('mapContainer');
  autocompleteElement = viewChild<ElementRef>('autocompleteElement');

  // State
  apiLoaded = signal<boolean>(false);
  loadingApi = signal<boolean>(true);
  searchQuery = '';
  manualLocationName = '';
  selectedLocationName = signal<string | undefined>(undefined);

  private map?: google.maps.Map;
  private marker?: any; // google.maps.marker.AdvancedMarkerElement
  private autocompleteListener?: google.maps.MapsEventListener;

  constructor() {
    // Sync initial name with signal
    effect(() => {
      const name = this.initialName();
      if (name && !this.selectedLocationName()) {
        this.selectedLocationName.set(name);
        this.manualLocationName = name;
      }
    });

    // Handle map updates when API is loaded OR theme changes
    effect(() => {
      const apiLoaded = this.apiLoaded();
      const container = this.mapContainer()?.nativeElement;
      const isDark = this.themeService.isDark();
      
      if (apiLoaded && container) {
        console.log('Re-initializing map for theme change:', isDark ? 'dark' : 'light');
        this.initMap();
      }
    });

    // React to initial coordinate changes (e.g. when loading an existing event)

    // React to initial coordinate changes (e.g. when loading an existing event)
    effect(() => {
      const lat = this.sanitizeCoordinate(this.initialLatitude());
      const lng = this.sanitizeCoordinate(this.initialLongitude());
      const name = this.initialName();
      
      if (this.map && lat && lng) {
        console.log('Inputs changed, updating map/marker:', { lat, lng, name });
        this.updateMarker(lat, lng, name || '');
        this.map.setCenter({ lat, lng });
      }
    });

    // Handle theme updates for non-map elements
    effect(() => {
      const isDark = this.themeService.isDark();
      
      // Update Autocomplete component color scheme
      const el = this.autocompleteElement()?.nativeElement as any;
      if (el) {
        el.setAttribute('color-scheme', isDark ? 'dark' : 'light');
      }
    });
  }

  ngAfterViewInit() {
    this.googleMapsService.load().pipe(take(1)).subscribe(loaded => {
      this.apiLoaded.set(loaded);
      this.loadingApi.set(false);
    });
  }

  ngOnDestroy() {
    // Clean up autocomplete listener if needed
    const el = this.autocompleteElement()?.nativeElement as any;
    if (el) {
      el.removeEventListener('gmp-select', this.handlePlaceSelect);
    }
  }

  private async initMap() {
    const container = this.mapContainer()?.nativeElement;
    if (!container) return;

    // Reset previous map state if re-initializing
    if (this.map) {
      this.marker = null; // Map recreation will handle new marker
      container.innerHTML = ''; // Clear container
    }

    const isDark = this.themeService.isDark();
    this.googleMapsService.getMapId().pipe(take(1)).subscribe(mapId => {
      // Use setTimeout to ensure container has dimensions
      setTimeout(() => {
        const lat = this.sanitizeCoordinate(this.initialLatitude()) || 52.0907;
        const lng = this.sanitizeCoordinate(this.initialLongitude()) || 5.1214;
        const zoom = this.initialLatitude() ? 15 : 7;

        console.log('Initializing map at:', { lat, lng, zoom, mapId });

        const mapOptions: any = {
          center: { lat, lng },
          zoom: zoom,
          mapId: mapId,
          colorScheme: isDark ? (google.maps as any).ColorScheme.DARK : (google.maps as any).ColorScheme.LIGHT,
          mapTypeControl: false,
          streetViewControl: false,
          fullscreenControl: false
        };

        // Only apply inline styles if no Map ID is provided, to avoid API warnings
        if (!mapId) {
          mapOptions.styles = [
            { featureType: 'poi', elementType: 'labels', stylers: [{ visibility: 'off' }] }
          ];
        }

        this.map = new google.maps.Map(container, mapOptions);

        if (this.initialLatitude() && this.initialLongitude()) {
          this.updateMarker(lat, lng, this.initialName() || '');
        }

        this.map.addListener('click', (e: google.maps.MapMouseEvent) => {
          if (e.latLng) {
            this.handleMapClick(e.latLng.lat(), e.latLng.lng());
          }
        });
      }, 50);
    });
  }

  private initAutocomplete() {
    // No longer needed as we use template binding (gmp-select)
  }

  public async handlePlaceSelect(event: any) {
    console.log('Place selected event:', event);
    const placePrediction = (event as any).placePrediction;
    if (!placePrediction) {
      console.warn('No place prediction found in event');
      return;
    }

    try {
      const place = await (placePrediction as any).toPlace();
      console.log('Place skeleton fetched:', place);
      
      // We must explicitly fetch the fields we need in the new Places API
      await (place as any).fetchFields({
        fields: ['location', 'displayName', 'formattedAddress']
      });
      
      console.log('Place details populated:', place);
      
      if (place?.location) {
        const lat = place.location.lat();
        const lng = place.location.lng();
        const name = (place as any).displayName || (place as any).formattedAddress || '';
        this.updateSelection(lat, lng, name);
        this.map?.setCenter({ lat, lng });
        this.map?.setZoom(17);
      } else {
        console.warn('Place still has no location after fetchFields:', place);
      }
    } catch (error) {
      console.error('Error fetching place details:', error);
    }
  }

  private async handleMapClick(lat: number, lng: number) {
    // Reverse geocode
    const geocoder = new google.maps.Geocoder();
    geocoder.geocode({ location: { lat, lng } }, (results, status) => {
      if (status === 'OK' && results?.[0]) {
        const name = results[0].formatted_address;
        this.updateSelection(lat, lng, name);
      } else {
        this.updateSelection(lat, lng, `${lat.toFixed(4)}, ${lng.toFixed(4)}`);
      }
    });
  }

  private updateSelection(lat: number, lng: number, name: string) {
    this.updateMarker(lat, lng, name);
    this.selectedLocationName.set(name);
    this.locationChanged.emit({ name, lat, lng });
  }

  private async updateMarker(lat: number, lng: number, name: string) {
    if (!this.map) return;

    const markerLib = await google.maps.importLibrary("marker") as any;
    const AdvancedMarkerElement = markerLib.AdvancedMarkerElement;

    if (this.marker) {
      this.marker.position = { lat, lng };
    } else {
      this.marker = new AdvancedMarkerElement({
        position: { lat, lng },
        map: this.map,
        gmpDraggable: true,
        title: name
      });

      this.marker.addListener('dragend', () => {
        const pos = this.marker?.position;
        if (pos) {
          // AdvancedMarker position can be LatLng or LatLngLiteral
          const lat = typeof pos.lat === 'function' ? pos.lat() : pos.lat;
          const lng = typeof pos.lng === 'function' ? pos.lng() : pos.lng;
          this.handleMapClick(lat, lng);
        }
      });
    }
  }

  onManualLocationChange() {
    this.locationChanged.emit({
      name: this.manualLocationName,
      lat: 0,
      lng: 0
    });
  }

  clearSelection() {
    if (this.marker) {
      this.marker.map = null;
      this.marker = undefined;
    }
    this.selectedLocationName.set(undefined);
    this.searchQuery = '';
    this.manualLocationName = '';
    this.locationChanged.emit(null);
  }

  private sanitizeCoordinate(coord: number | undefined): number | undefined {
    if (coord === undefined || coord === null || isNaN(coord)) return undefined;
    if (coord === 0) return 0;
    
    // Standard coordinates are between -180 and 180
    if (Math.abs(coord) <= 180) return coord;
    
    // If coordinate is ridiculously large, it was likely mangled by locale issues (dot vs comma)
    // We attempt to "heal" it by scaling it down
    let fixed = coord;
    while (Math.abs(fixed) > 180) {
      fixed /= 10;
    }
    
    console.warn(`Sanitized coordinate from ${coord} to ${fixed}`);
    return fixed;
  }
}
