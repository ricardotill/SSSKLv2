import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Announcement, AnnouncementCreateDto, AnnouncementUpdateDto, PaginatedAnnouncements } from '../models/announcement.model';

@Injectable({
  providedIn: 'root'
})
export class AnnouncementService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/announcement';

  getAnnouncements(skip: number = 0, take: number = 15): Observable<PaginatedAnnouncements> {
    return this.http.get<PaginatedAnnouncements>(this.apiUrl, {
      params: { skip, take }
    });
  }

  getAnnouncement(id: string): Observable<Announcement> {
    return this.http.get<Announcement>(`${this.apiUrl}/${id}`);
  }

  createAnnouncement(dto: AnnouncementCreateDto): Observable<Announcement> {
    return this.http.post<Announcement>(this.apiUrl, dto);
  }

  updateAnnouncement(id: string, dto: AnnouncementUpdateDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto);
  }

  deleteAnnouncement(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
