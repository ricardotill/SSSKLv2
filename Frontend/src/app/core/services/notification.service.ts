import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { NotificationDto } from '../models/notification.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/api/v1/notifications`;
  
  public unreadCount = signal<number>(0);

  getNotifications(unreadOnly: boolean = false, skip: number = 0, take: number = 20): Observable<NotificationDto[]> {
    let params = new HttpParams()
      .set('unreadOnly', unreadOnly.toString())
      .set('skip', skip.toString())
      .set('take', take.toString());

    return this.http.get<NotificationDto[]>(this.apiUrl, { params });
  }

  fetchUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/unread-count`).pipe(
      tap(count => this.unreadCount.set(count))
    );
  }

  markAsRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/read`, {}).pipe(
      tap(() => {
        // Decrease count optimistically if it's > 0
        if (this.unreadCount() > 0) {
          this.unreadCount.update(c => c - 1);
        }
      })
    );
  }

  markAllAsRead(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/read-all`, {}).pipe(
      tap(() => this.unreadCount.set(0))
    );
  }

  sendCustomNotification(dto: { title: string, message: string, linkUri?: string, fanOut: boolean, userIds?: string[] }): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/custom`, dto);
  }
}
