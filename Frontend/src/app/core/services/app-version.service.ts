import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';

// Version of this build: the release tag (v1.2.3 → 1.2.3), injected by ng-env.js from APP_VERSION.
export const CURRENT_APP_VERSION = typeof APP_VERSION !== 'undefined' ? APP_VERSION : '0.0.0';

export interface AppVersionResponse {
  version: string;
}

@Injectable({
  providedIn: 'root'
})
export class AppVersionService {
  private readonly http = inject(HttpClient);
  private readonly versionUrl = '/api/v1/public/version';

  readonly version = signal(CURRENT_APP_VERSION);

  constructor() {
    this.refresh().subscribe();
  }

  refresh(): Observable<AppVersionResponse> {
    return this.http.get<AppVersionResponse>(this.versionUrl).pipe(
      tap(response => this.version.set(response.version)),
      catchError(() => {
        const fallback = { version: CURRENT_APP_VERSION };
        this.version.set(fallback.version);
        return of(fallback);
      })
    );
  }
}
