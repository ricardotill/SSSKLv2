import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LeaderboardEntryDto } from '../models/leaderboard.model';

@Injectable({
  providedIn: 'root'
})
export class LeaderboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/leaderboard';

  getLeaderboard(productId: string): Observable<LeaderboardEntryDto[]> {
    return this.http.get<LeaderboardEntryDto[]>(`${this.baseUrl}/${productId}`);
  }

  getMonthlyLeaderboard(productId: string): Observable<LeaderboardEntryDto[]> {
    return this.http.get<LeaderboardEntryDto[]>(`${this.baseUrl}/monthly/${productId}`);
  }

  get12HourLeaderboard(productId: string): Observable<LeaderboardEntryDto[]> {
    return this.http.get<LeaderboardEntryDto[]>(`${this.baseUrl}/12hour/${productId}`);
  }
}
