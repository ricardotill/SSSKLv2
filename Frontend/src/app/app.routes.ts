import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component'),
    title: 'Login - SSSKLv2'
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
        path: 'orders',
        loadComponent: () => import('./features/orders/orders.component'),
        canActivate: [authGuard],
        title: 'Orders - Admin Panel'
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
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
