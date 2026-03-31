import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedTopUps, TopUpDto, TopUpCreateDto } from '../../../core/models/top-up.model';

@Injectable({
  providedIn: 'root'
})
export class TopUpService {
  private http = inject(HttpClient);
  private readonly API_URL = '/api/v1/TopUp';

  getPersonalTopUps(skip: number = 0, take: number = 15): Observable<PaginatedTopUps> {
    return this.http.get<PaginatedTopUps>(`${this.API_URL}/personal`, {
      params: { skip, take }
    });
  }

  getTopUps(skip: number = 0, take: number = 15): Observable<PaginatedTopUps> {
    return this.http.get<PaginatedTopUps>(this.API_URL, {
      params: { skip, take }
    });
  }

  createTopUp(dto: TopUpCreateDto): Observable<TopUpDto> {
    return this.http.post<TopUpDto>(this.API_URL, dto);
  }

  deleteTopUp(id: string): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }
}
