import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { unauthGuard } from './core/auth/unauth.guard'; // Assume I will create this snippet

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component'),
    canActivate: [unauthGuard],
    title: 'Login - SSSKLv2'
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component'),
    canActivate: [unauthGuard],
    title: 'Register - SSSKLv2'
  },
  {
    path: '',
    loadComponent: () => import('./layout/main-layout/main-layout.component'),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/homepage/homepage.component'),
        title: 'Homepage'
      },
      {
        path: 'users',
        loadComponent: () => import('./features/users/users.component'),
        canActivate: [authGuard],
        title: 'Users - Admin Panel'
      },
      {
        path: 'products',
        loadComponent: () => import('./features/products/products.component'),
        canActivate: [authGuard],
        title: 'Products - Admin Panel'
      },
      {
        path: 'pos',
        loadComponent: () => import('./features/pos/pos.component'),
        canActivate: [authGuard],
        title: 'Bestellen - Admin Panel'
      },
      {
        path: 'announcements',
        loadComponent: () => import('./features/announcements/announcements.component'),
        canActivate: [authGuard],
        title: 'Announcements - Admin Panel'
      },
      {
        path: 'achievements',
        loadComponent: () => import('./features/achievements/achievements.component'),
        canActivate: [authGuard],
        title: 'Achievements - Admin Panel'
      },
      {
        path: 'topups',
        loadComponent: () => import('./features/topups/topups.component'),
        canActivate: [authGuard],
        title: 'Top-Ups - Admin Panel'
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/settings/settings.component'),
        canActivate: [authGuard],
        title: 'Settings - Admin Panel'
      },
      {
        path: 'orders',
        canActivate: [authGuard],
        children: [
          {
            path: 'personal',
            loadComponent: () => import('./features/orders/personal-orders.component'),
            title: 'My Orders'
          }
        ]
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
