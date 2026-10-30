import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { GlobalSetting, GlobalSettingUpdateDto } from '../../../core/models/global-settings.model';
import { Observable, catchError, map, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class GlobalSettingsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `/api/v1/GlobalSettings`;

  /** Emits null when the setting hasn't been set (the API answers 204). */
  getSetting(key: string): Observable<GlobalSetting | null> {
    return this.http.get<GlobalSetting | null>(`${this.apiUrl}/${key}`);
  }

  updateSetting(key: string, dto: GlobalSettingUpdateDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${key}`, dto);
  }

  settingExists(key: string): Observable<boolean> {
    // The API returns 403 for write-only keys (password), 204 if not set, 200 if set.
    // Anything but 204 means the setting exists.
    return this.http.get(`${this.apiUrl}/${key}`, { observe: 'response' }).pipe(
      map(res => res.status !== 204),
      catchError(() => of(true))
    );
  }

  sendTestEmail(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/test-email`, {});
  }
}
