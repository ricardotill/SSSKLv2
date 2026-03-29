import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from './auth.service';
import { toObservable } from '@angular/core/rxjs-interop';
import { filter, map, take } from 'rxjs';

export const roleGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    const requiredRoles = route.data?.['roles'] as Array<string>;
    
    // Fallback if no roles are required
    if (!requiredRoles || requiredRoles.length === 0) {
        return true;
    }

    // Wait for the auth service to be initialized before making a decision
    return toObservable(authService.isInitialized).pipe(
        filter(initialized => initialized),
        take(1),
        map(() => {
            const currentUser = authService.currentUser();
            
            if (!currentUser) {
                return router.createUrlTree(['/']);
            }

            const userRoles = currentUser.roles || [];
            const hasRole = requiredRoles.some(role => userRoles.includes(role));

            if (hasRole) {
                return true;
            }

            return router.createUrlTree(['/']);
        })
    );
};
