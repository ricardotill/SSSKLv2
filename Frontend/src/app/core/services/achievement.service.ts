import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { Achievement, AchievementEntry, AchievementListing, PaginationObject, AchievementUpdateDto } from '../models/achievement.model';

@Injectable({
  providedIn: 'root'
})
export class AchievementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/achievement';

  getAchievements(skip: number = 0, take: number = 100): Observable<PaginationObject<Achievement>> {
    return this.http.get<PaginationObject<Achievement>>(this.baseUrl, {
      params: { skip, take }
    });
  }

  getAchievementById(id: string): Observable<Achievement> {
    return this.http.get<Achievement>(`${this.baseUrl}/${id}`);
  }

  createAchievement(formData: FormData): Observable<void> {
    return this.http.post<void>(this.baseUrl, formData);
  }

  updateAchievement(dto: AchievementUpdateDto): Observable<void> {
    return this.http.put<void>(this.baseUrl, dto);
  }

  deleteAchievement(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  awardAchievementToUser(userId: string, achievementId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/award/${userId}/${achievementId}`, {});
  }

  awardAchievementToAllUsers(achievementId: string): Observable<number> {
    return this.http.post<number>(`${this.baseUrl}/award/all/${achievementId}`, {});
  }

  getAchievementEntries(userId: string): Observable<AchievementEntry[]> {
    return this.http.get<AchievementEntry[]>(`${this.baseUrl}/entries/${userId}`);
  }

  getAllForUser(userId: string): Observable<AchievementListing[]> {
    return forkJoin({
        // Fetch the master list of achievements using the /personal endpoint (accessible to all logged-in users)
        // We only use this for the defined achievements, ignoring its specific completion status.
        all: this.http.get<AchievementListing[]>(`${this.baseUrl}/personal`),
        // Fetch specific entries for the target user
        entries: this.getAchievementEntries(userId)
    }).pipe(
        map(({ all, entries }) => {
            return all.map((ac: AchievementListing) => {
                // Find if the target user has an entry for this achievement by matching the name
                const userEntry = entries.find(e => e.achievementName === ac.name);
                return {
                    name: ac.name,
                    description: ac.description,
                    dateAdded: userEntry ? userEntry.dateAdded : undefined,
                    imageUrl: ac.imageUrl, // Map the image URL from the master list
                    completed: !!userEntry
                } as AchievementListing;
            });
        })
    );
  }

  getPersonalAchievementEntries(): Observable<AchievementEntry[]> {
    return this.http.get<AchievementEntry[]>(`${this.baseUrl}/entries/personal`);
  }

  getUnseenAchievementEntries(): Observable<AchievementEntry[]> {
    return this.http.get<AchievementEntry[]>(`${this.baseUrl}/entries/unseen`);
  }

  deleteAchievementEntries(ids: string[]): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/entries/delete`, ids);
  }
}
