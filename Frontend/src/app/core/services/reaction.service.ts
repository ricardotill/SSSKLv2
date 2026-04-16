import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ReactionDto, ToggleReactionRequest, ReactionTargetType } from '../models/reaction.model';

@Injectable({
  providedIn: 'root'
})
export class ReactionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/v1/Reactions';

  toggleReaction(targetId: string, targetType: ReactionTargetType, content: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/toggle`, { targetId, targetType, content });
  }

  getReactions(targetId: string, targetType: ReactionTargetType): Observable<ReactionDto[]> {
    return this.http.get<ReactionDto[]>(`${this.apiUrl}/${targetId}/${targetType}`);
  }

  getTimeline(skip: number = 0, take: number = 10): Observable<ReactionDto[]> {
    return this.http.get<ReactionDto[]>(`${this.apiUrl}/timeline`, {
      params: { skip, take }
    });
  }

  deleteReaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
