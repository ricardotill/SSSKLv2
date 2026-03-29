import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PublicService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/public';

  getDomain(): Observable<string> {
    return this.http.get<string>(`${this.baseUrl}/domain`, { responseType: 'text' as 'json' });
  }
}
