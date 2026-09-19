import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApplicationUserDetailedDto, ApplicationUserDto, ApplicationUserUpdateDto, PaginatedUsers } from '../../../core/models/application-user.model';
import { UserStat } from '../../../core/models/user-stat.model';
import { RecalculationJob } from '../../../core/models/recalculation-job.model';

@Injectable({
  providedIn: 'root'
})
export class ApplicationUserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/applicationuser';

  /**
   * Used to retrieve all users (with optional pagination).
   * For the management page, we use high 'take' to get everything or depend on PrimeNG lazy load.
   */
  getUsers(skip: number = 0, take: number = 1000): Observable<PaginatedUsers> {
    return this.http.get<PaginatedUsers>(this.baseUrl, {
      params: { skip, take }
    });
  }

  getUser(id: string): Observable<ApplicationUserDetailedDto> {
    return this.http.get<ApplicationUserDetailedDto>(`${this.baseUrl}/${id}`);
  }

  updateUser(id: string, dto: ApplicationUserUpdateDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
  
  deleteProfilePicture(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/profile-picture`);
  }

  getAdminUsers(skip: number = 0, take: number = 1000): Observable<PaginatedUsers> {
    return this.http.get<PaginatedUsers>(`${this.baseUrl}/admin`, {
      params: { skip, take }
    });
  }

  getUserStats(id: string): Observable<UserStat> {
    return this.http.get<UserStat>(`${this.baseUrl}/${id}/stats`);
  }

  recalculateUserStats(id: string): Observable<UserStat> {
    return this.http.post<UserStat>(`${this.baseUrl}/${id}/stats/recalculate`, {});
  }

  /**
   * Starts an async, admin-only job that recalculates stats for every user.
   * Resolves with the job status even if a job was already running (HTTP 409).
   */
  startRecalculateAllStats(): Observable<RecalculationJob> {
    return this.http.post<RecalculationJob>(`${this.baseUrl}/stats/recalculate-all`, {});
  }

  getRecalculateAllStatsStatus(jobId: string): Observable<RecalculationJob> {
    return this.http.get<RecalculationJob>(`${this.baseUrl}/stats/recalculate-all/${jobId}`);
  }

  getLatestRecalculateAllStatsStatus(): Observable<RecalculationJob> {
    return this.http.get<RecalculationJob>(`${this.baseUrl}/stats/recalculate-all/latest`);
  }
}
