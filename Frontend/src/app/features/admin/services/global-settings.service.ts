import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { GlobalSetting, GlobalSettingUpdateDto } from '../../../core/models/global-settings.model';
import { Observable } from 'rxjs';

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
}
