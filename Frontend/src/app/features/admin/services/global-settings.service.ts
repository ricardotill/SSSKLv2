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

  getSetting(key: string): Observable<GlobalSetting> {
    return this.http.get<GlobalSetting>(`${this.apiUrl}/${key}`);
  }

  updateSetting(key: string, dto: GlobalSettingUpdateDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${key}`, dto);
  }

  settingExists(key: string): Observable<boolean> {
    // The API returns 403 for write-only keys (password), 404 if not set, 200 if set.
    // Any non-404 response means the setting exists.
    return this.http.get(`${this.apiUrl}/${key}`, { observe: 'response' }).pipe(
      map(() => true),
      catchError(err => of(err.status !== 404))
    );
  }

  sendTestEmail(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/test-email`, {});
  }
}
