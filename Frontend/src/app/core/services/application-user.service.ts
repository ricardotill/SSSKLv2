import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApplicationUserDetailedDto, ApplicationUserDto, ApplicationUserUpdateDto, PaginatedUsers } from '../models/application-user.model';

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

  getObscuredUsers(): Observable<ApplicationUserDto[]> {
    return this.http.get<ApplicationUserDto[]>(`/api/v1/applicationuser/obscured`);
  }
}
