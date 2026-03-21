import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';

let isRefreshing = false;
let refreshTokenSubject: BehaviorSubject<string | null> = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
    const authService = inject(AuthService);
    const token = authService.getAccessToken();

    let authReq = req;
    // Don't add token to identity endpoints like login/refresh/register
    if (token && !req.url.includes(`/api/v1/identity/login`) && !req.url.includes(`/api/v1/identity/refresh`)) {
        authReq = addTokenHeader(req, token);
    }

    return next(authReq).pipe(
        catchError(error => {
            if (error instanceof HttpErrorResponse && error.status === 401 && !authReq.url.includes(`/api/v1/identity/login`)) {
                return handle401Error(authReq, next, authService);
            }
            return throwError(() => error);
        })
    );
};

function addTokenHeader(request: HttpRequest<any>, token: string) {
    return request.clone({
        setHeaders: {
            Authorization: `Bearer ${token}`
        }
    });
}

function handle401Error(request: HttpRequest<any>, next: HttpHandlerFn, authService: AuthService) {
    if (!isRefreshing) {
        isRefreshing = true;
        refreshTokenSubject.next(null);

        const refreshToken = authService.getRefreshToken();

        if (refreshToken) {
            return authService.refreshTokens().pipe(
                switchMap((tokenResponse: any) => {
                    isRefreshing = false;
                    refreshTokenSubject.next(tokenResponse.accessToken);
                    return next(addTokenHeader(request, tokenResponse.accessToken));
                }),
                catchError((err) => {
                    isRefreshing = false;
                    authService.logout();
                    return throwError(() => err);
                })
            );
        } else {
            isRefreshing = false;
            authService.logout();
            return throwError(() => new Error('Session expired and no refresh token available'));
        }
    }

    return refreshTokenSubject.pipe(
        filter(token => token !== null),
        take(1),
        switchMap((token) => next(addTokenHeader(request, token!)))
    );
}
