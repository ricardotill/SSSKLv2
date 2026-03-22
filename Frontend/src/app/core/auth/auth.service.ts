import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, map, tap } from 'rxjs/operators';
import { Observable, of, throwError } from 'rxjs';

export interface AccessTokenResponse {
    tokenType: string | null;
    accessToken: string;
    expiresIn: number;
    refreshToken: string;
}

export interface ApplicationUserDetailedDto {
    id: string;
    userName: string;
    email: string | null;
    emailConfirmed: boolean;
    phoneNumber: string | null;
    phoneNumberConfirmed: boolean;
    name: string | null;
    surname: string | null;
    fullName: string;
    saldo: number;
    lastOrdered: string;
    profilePictureBase64: string | null;
    roles: string[];
}

export interface ApplicationUserSelfUpdateDto {
    phoneNumber?: string | null;
    name?: string | null;
    surname?: string | null;
}

export interface InfoRequest {
    newEmail?: string | null;
    newPassword?: string | null;
    oldPassword?: string | null;
}

export interface InfoResponse {
    email: string;
    isEmailConfirmed: boolean;
}

export interface TwoFactorRequest {
    enable?: boolean | null;
    twoFactorCode?: string | null;
    resetSharedKey?: boolean;
    resetRecoveryCodes?: boolean;
    forgetMachine?: boolean;
}

export interface TwoFactorResponse {
    sharedKey: string;
    recoveryCodesLeft: number;
    recoveryCodes: string[] | null;
    isTwoFactorEnabled: boolean;
    isMachineRemembered: boolean;
}

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private http = inject(HttpClient);
    private router = inject(Router);

    private readonly API_URL = `/api/v1/identity`;
    private readonly USER_API_URL = `/api/v1/ApplicationUser`;

    private currentUserSignal = signal<ApplicationUserDetailedDto | null>(null);

    // Public computed signals for components to consume
    currentUser = computed(() => this.currentUserSignal());
    isAuthenticated = computed(() => !!this.currentUserSignal() || !!this.getAccessToken());

    constructor() {
        // Defer initAuth to prevent circular dependency with HttpInterceptor
        setTimeout(() => {
            this.initAuth();
        });
    }

    private initAuth(): void {
        const token = this.getAccessToken();
        if (token) {
            // If we have a token, fetch user info to populate the current user signal
            this.fetchCurrentUser().subscribe({
                error: (err) => console.error('Failed to fetch user:', err)
            });
        }
    }

    login(credentials: { userName?: string, password?: string, twoFactorCode?: string, twoFactorRecoveryCode?: string }, rememberMe: boolean = false): Observable<AccessTokenResponse> {
        return this.http.post<AccessTokenResponse>(`${this.API_URL}/login`, credentials).pipe(
            tap(response => {
                this.setTokens(response, rememberMe);
            }),
            tap(() => {
                this.fetchCurrentUser().subscribe({
                    error: (err) => console.error('Failed to fetch user:', err)
                });
            })
        );
    }

    register(userData: any): Observable<any> {
        return this.http.post(`${this.API_URL}/register`, userData);
    }

    logout(): void {
        const refreshToken = this.getRefreshToken();
        if (refreshToken) {
            // Optional: call logout on backend
            // this.http.post(`${this.API_URL}/logout`, undefined, { params: { useCookies: 'false' } }).subscribe();
        }
        this.clearTokens();
        this.currentUserSignal.set(null);
        this.router.navigate(['/']);
    }

    refreshTokens(): Observable<AccessTokenResponse> {
        const refreshToken = this.getRefreshToken();
        if (!refreshToken) {
            this.logout();
            return throwError(() => new Error('No refresh token available'));
        }

        return this.http.post<AccessTokenResponse>(`${this.API_URL}/refresh`, { refreshToken }).pipe(
            tap(response => {
                // Determine which storage currently holds the refresh token to maintain the same storage behavior
                const rememberMe = !!localStorage.getItem('refresh_token');
                this.setTokens(response, rememberMe);
            }),
            catchError(error => {
                this.logout();
                return throwError(() => error);
            })
        );
    }

    private fetchCurrentUser(): Observable<ApplicationUserDetailedDto> {
        return this.http.get<ApplicationUserDetailedDto>(`${this.USER_API_URL}/me`).pipe(
            tap(user => this.currentUserSignal.set(user))
        );
    }

    refreshCurrentUser(): void {
        this.fetchCurrentUser().subscribe({
            error: (err) => console.error('Failed to refresh user:', err)
        });
    }

    updateMe(data: ApplicationUserSelfUpdateDto): Observable<any> {
        return this.http.put(`${this.USER_API_URL}/me`, data).pipe(
            tap(() => this.refreshCurrentUser())
        );
    }

    getIdentityInfo(): Observable<InfoResponse> {
        return this.http.get<InfoResponse>(`${this.API_URL}/manage/info`);
    }

    updateIdentityInfo(data: InfoRequest): Observable<InfoResponse> {
        return this.http.post<InfoResponse>(`${this.API_URL}/manage/info`, data);
    }

    manage2fa(data: TwoFactorRequest): Observable<TwoFactorResponse> {
        return this.http.post<TwoFactorResponse>(`${this.API_URL}/manage/2fa`, data);
    }

    downloadPersonalData(): Observable<Blob> {
        return this.http.post(`${this.USER_API_URL}/me/personaldata`, {}, { responseType: 'blob' });
    }

    getAccessToken(): string | null {
        return localStorage.getItem('access_token') || sessionStorage.getItem('access_token');
    }

    getRefreshToken(): string | null {
        return localStorage.getItem('refresh_token') || sessionStorage.getItem('refresh_token');
    }

    private setTokens(response: AccessTokenResponse, rememberMe: boolean): void {
        const storage = rememberMe ? localStorage : sessionStorage;
        storage.setItem('access_token', response.accessToken);
        storage.setItem('refresh_token', response.refreshToken);
    }

    private clearTokens(): void {
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        sessionStorage.removeItem('access_token');
        sessionStorage.removeItem('refresh_token');
    }
}
