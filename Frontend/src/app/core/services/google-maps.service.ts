import { inject, Injectable } from '@angular/core';
import { setOptions, importLibrary } from '@googlemaps/js-api-loader';
import { GlobalSettingsService } from '../../features/admin/services/global-settings.service';
import { catchError, map, Observable, of, shareReplay, switchMap, from, forkJoin } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class GoogleMapsService {
  private readonly globalSettingsService = inject(GlobalSettingsService);
  private initialized = false;
  private loadObservable$: Observable<boolean> | null = null;

  /**
   * Loads the Google Maps API using the key from GlobalSettings.
   * Returns an observable that emits true if loaded, false otherwise.
   */
  load(): Observable<boolean> {
    if (this.loadObservable$) {
      return this.loadObservable$;
    }

    this.loadObservable$ = this.globalSettingsService.getSetting('GoogleMapsApiKey').pipe(
      map(setting => setting.value),
      switchMap(apiKey => {
        if (!apiKey) {
          console.warn('GoogleMapsApiKey not found in GlobalSettings');
          return of(false);
        }

        if (!this.initialized) {
          setOptions({
            key: apiKey,
            v: 'weekly',
            libraries: ['places']
          });
          this.initialized = true;
        }

        // Import required libraries to ensure they are available globally
        return forkJoin([
          from(importLibrary('maps')),
          from(importLibrary('places')),
          from(importLibrary('marker')) // New markers are in the 'marker' library
        ]).pipe(
          map(() => true),
          catchError(err => {
            console.error('Failed to load Google Maps libraries', err);
            return of(false);
          })
        );
      }),
      catchError(err => {
        console.error('Error fetching GoogleMapsApiKey', err);
        return of(false);
      }),
      shareReplay(1)
    );

    return this.loadObservable$;
  }

  /**
   * Fetches the Map ID from settings.
   */
  getMapId(): Observable<string | undefined> {
    return this.globalSettingsService.getSetting('GoogleMapsMapId').pipe(
      map(setting => setting.value || undefined),
      catchError(() => of(undefined))
    );
  }

  /**
   * Checks if the API key is available.
   */
  hasApiKey(): Observable<boolean> {
    return this.globalSettingsService.getSetting('GoogleMapsApiKey').pipe(
      map(setting => !!setting.value),
      catchError(() => of(false))
    );
  }
}
