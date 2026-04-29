import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { Observable, throwError } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';

export const authInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
    const authService = inject(AuthService);
    const token = authService.getAccessToken();
    const isPublicIdentityEndpoint = req.url.includes('/api/v1/identity/') && !req.url.includes('/manage/') && !req.url.includes('/PasskeyCreation');

    let authReq = req;
    // Don't add token to public identity endpoints.
    if (token && !isPublicIdentityEndpoint) {
        authReq = addTokenHeader(req, token);
    }

    return next(authReq).pipe(
        catchError(error => {
            if (error instanceof HttpErrorResponse && error.status === 401 && !isPublicIdentityEndpoint) {
                return handle401Error(authReq, next, authService);
            }
            return throwError(() => error);
        })
    );
};

function handle401Error(request: HttpRequest<any>, next: HttpHandlerFn, authService: AuthService) {
    if (!authService.isRefreshing) {
        authService.isRefreshing = true;
        authService.refreshTokenSubject.next(null);

        return authService.refreshTokens().pipe(
            switchMap((tokenResponse) => {
                authService.isRefreshing = false;
                authService.refreshTokenSubject.next(tokenResponse.accessToken);
                return next(addTokenHeader(request, tokenResponse.accessToken));
            }),
            catchError((err) => {
                authService.isRefreshing = false;
                authService.logout();
                return throwError(() => err);
            })
        );
    } else {
        return authService.refreshTokenSubject.pipe(
            filter(token => token !== null),
            take(1),
            switchMap((token) => next(addTokenHeader(request, token!)))
        );
    }
}

function addTokenHeader(request: HttpRequest<any>, token: string) {
    return request.clone({
        setHeaders: {
            Authorization: `Bearer ${token}`
        }
    });
}
