import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from './auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    const requiredRoles = route.data?.['roles'] as Array<string>;
    
    // Fallback if no roles are required
    if (!requiredRoles || requiredRoles.length === 0) {
        return true;
    }

    const currentUser = authService.currentUser();
    
    // Wait for the user to be loaded? Assuming currentUser is set since authGuard runs first
    if (!currentUser) {
        return router.createUrlTree(['/']);
    }

    const userRoles = currentUser.roles || [];
    
    const hasRole = requiredRoles.some(role => userRoles.includes(role));

    if (hasRole) {
        return true;
    }

    return router.createUrlTree(['/']);
};
