import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { QuoteDto, QuoteCreateDto, QuoteUpdateDto, PaginationObject } from '../../../core/models/quote.model';

@Injectable({
  providedIn: 'root'
})
export class QuoteService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/Quote';

  getQuotes(skip: number = 0, take: number = 15, targetUserId?: string): Observable<PaginationObject<QuoteDto>> {
    const params: any = { skip, take };
    if (targetUserId) {
      params.targetUserId = targetUserId;
    }
    return this.http.get<PaginationObject<QuoteDto>>(this.apiUrl, { params });
  }

  getQuote(id: string): Observable<QuoteDto> {
    return this.http.get<QuoteDto>(`${this.apiUrl}/${id}`);
  }

  createQuote(dto: QuoteCreateDto): Observable<QuoteDto> {
    return this.http.post<QuoteDto>(this.apiUrl, dto);
  }

  updateQuote(id: string, dto: QuoteUpdateDto): Observable<QuoteDto> {
    return this.http.put<QuoteDto>(`${this.apiUrl}/${id}`, dto);
  }

  deleteQuote(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  toggleVote(id: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiUrl}/${id}/vote`, {});
  }
}
