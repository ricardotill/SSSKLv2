import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';

export const CURRENT_APP_VERSION = '3.8.3';

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
