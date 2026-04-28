import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of } from 'rxjs';

/**
 * Guard that checks whether the current user has access to the Quotes feature.
 * It probes GET /api/v1/quote with a take=0 query.
 * - 200 → allow navigation
 * - 403 → redirect to home (feature is role-restricted and user lacks access)
 * - other errors (network etc.) → allow through; the page will handle errors itself
 */
export const quotesGuard: CanActivateFn = () => {
    const http = inject(HttpClient);
    const router = inject(Router);

    return http.get('/api/v1/quote', { params: { skip: '0', take: '1' } }).pipe(
        map(() => true),
        catchError(err => {
            if (err.status === 403) {
                return of(router.createUrlTree(['/']));
            }
            // Allow through for other errors (401 handled by auth interceptor, etc.)
            return of(true);
        })
    );
};
