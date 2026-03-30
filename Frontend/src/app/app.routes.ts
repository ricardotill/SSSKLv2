import { isDevMode } from '@angular/core';
import { Routes } from '@angular/router';
import { MessageService, ConfirmationService } from 'primeng/api';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { unauthGuard } from './core/auth/unauth.guard'; // Assume I will create this snippet

const devRoutes = isDevMode() ? [
  {
    path: 'leaderboard/livedisplay/dev',
    loadComponent: () => import('./features/leaderboard/live-display/dev-live-display.component').then(m => m.DevLiveDisplayComponent),
    title: 'Dev: Live Display Scaling Test - SSSKL'
  }
] : [];

export const routes: Routes = [
  ...devRoutes,
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component'),
    canActivate: [unauthGuard],
    title: 'Login - SSSKL'
  },
  {
    path: 'error',
    loadComponent: () => import('./features/error/error.component'),
    title: 'Fout - SSSKL'
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component'),
    canActivate: [unauthGuard],
    title: 'Registreren - SSSKL'
  },
  {
    path: 'leaderboard/livedisplay',
    loadComponent: () => import('./features/leaderboard/live-display/live-display.component').then(m => m.LiveDisplayComponent),
    title: 'Live Leaderboard - SSSKL'
  },
  {
    path: 'leaderboard/livedisplay/:id',
    loadComponent: () => import('./features/leaderboard/live-display/live-display.component').then(m => m.LiveDisplayComponent),
    title: 'Live Leaderboard - SSSKL'
  },
  {
    path: '',
    loadComponent: () => import('./layout/main-layout/main-layout.component').then(m => m.MainLayoutComponent),
    providers: [MessageService, ConfirmationService],
    children: [
      {
        path: 'leaderboard',
        loadComponent: () => import('./features/leaderboard/leaderboard.component'),
        title: 'Leaderboard - SSSKL'
      },
      {
        path: '',
        loadComponent: () => import('./features/homepage/homepage.component'),
        canActivate: [unauthGuard],
        title: 'Home - SSSKL'
      },
      {
        path: 'about',
        loadComponent: () => import('./features/homepage/homepage.component'),
        canActivate: [authGuard],
        title: 'Over SSSKL - SSSKL'
      },
      {
        path: 'pos',
        loadComponent: () => import('./features/pos/pos.component'),
        canActivate: [authGuard],
        title: 'Bestellen - SSSKL'
      },
      {
        path: 'admin',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Admin', 'Kiosk'] },
        loadChildren: () => import('./features/admin/admin.routes')
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/settings/settings.component'),
        canActivate: [authGuard],
        title: 'Instellingen - SSSKL'
      },
      {
        path: 'orders',
        canActivate: [authGuard],
        children: [
          {
            path: 'personal',
            loadComponent: () => import('./features/orders/personal-orders.component'),
            title: 'Mijn Bestellingen - SSSKL'
          },
          {
            path: 'saldo',
            loadComponent: () => import('./features/top-ups/personal-top-ups.component'),
            title: 'Mijn Saldo - SSSKL'
          }
        ]
      },
      {
        path: 'users',
        loadComponent: () => import('./features/users/users-overview.component'),
        canActivate: [authGuard],
        title: 'Gebruikersoverzicht - SSSKL'
      },
      {
        path: 'achievements',
        loadComponent: () => import('./features/achievements/achievements.component'),
        canActivate: [authGuard],
        title: 'Achievements - SSSKL'
      },
      {
        path: 'events',
        children: [
          {
            path: '',
            loadComponent: () => import('./features/events/events.component'),
            canActivate: [authGuard],
            title: 'Evenementen - SSSKL'
          },
          {
            path: 'new',
            loadComponent: () => import('./features/events/event-edit/event-edit.component'),
            canActivate: [authGuard, roleGuard],
            data: { roles: ['Admin', 'User'] },
            title: 'Evenement Toevoegen - SSSKL'
          },
          {
            path: ':id',
            loadComponent: () => import('./features/events/event-detail/event-detail.component'),
            title: 'Evenement Details - SSSKL'
          },
          {
            path: ':id/edit',
            loadComponent: () => import('./features/events/event-edit/event-edit.component'),
            canActivate: [authGuard, roleGuard],
            data: { roles: ['Admin', 'User'] },
            title: 'Evenement Bewerken - SSSKL'
          }
        ]
      }
    ]
  },
  { path: '404', loadComponent: () => import('./features/error/error.component'), data: { code: '404' }, title: 'Pagina niet gevonden - SSSKL' },
  { path: '**', redirectTo: '404' }
];
