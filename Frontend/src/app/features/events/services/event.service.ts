import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { from, Observable, switchMap } from 'rxjs';
import { EventDto, EventResponseStatus, PaginationObject } from '../../../core/models/event.model';
import { fileToBase64 } from '../../../shared/utils/file-to-base64';

@Injectable({
  providedIn: 'root'
})
export class EventService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/Events';

  getEvents(skip: number = 0, take: number = 15, futureOnly: boolean = true): Observable<PaginationObject<EventDto>> {
    return this.http.get<PaginationObject<EventDto>>(this.apiUrl, {
      params: { skip, take, futureOnly }
    });
  }

  getPublicEvents(skip: number = 0, take: number = 15, futureOnly: boolean = true, requiredRole?: string): Observable<PaginationObject<EventDto>> {
    const params: any = { skip, take, futureOnly };
    if (requiredRole) params.requiredRole = requiredRole;
    return this.http.get<PaginationObject<EventDto>>(`${this.apiUrl}/public`, { params });
  }

  getEvent(id: string): Observable<EventDto> {
    return this.http.get<EventDto>(`${this.apiUrl}/${id}`);
  }

  createEvent(formData: FormData): Observable<EventDto> {
    return this.http.post<EventDto>(this.apiUrl, formData);
  }

  updateEvent(id: string, formData: FormData): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}`, formData);
  }

  updateEventImage(id: string, file: File): Observable<void> {
    const fileName = file.name || 'event-image';
    const contentType = file.type || 'application/octet-stream';

    return from(fileToBase64(file)).pipe(
      switchMap(base64Content =>
        this.http.post<void>(`${this.apiUrl}/${id}/image`, {
          fileName,
          contentType,
          base64Content
        })
      )
    );
  }

  deleteEvent(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  rsvp(id: string, status: EventResponseStatus): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/rsvp`, { status });
  }
}
