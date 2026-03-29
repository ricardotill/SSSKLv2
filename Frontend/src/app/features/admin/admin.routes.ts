import { Routes } from '@angular/router';
import { roleGuard } from '../../core/auth/role.guard';

export const routes: Routes = [
  {
    path: '',
    children: [
      {
        path: 'products',
        loadComponent: () => import('./products/products.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        title: 'Producten Beheren - SSSKL'
      },
      {
        path: 'topups',
        loadComponent: () => import('./topups/topups.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        title: 'Opwaarderingen Beheren - SSSKL'
      },
      {
        path: 'orders',
        loadComponent: () => import('./orders/orders.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Kiosk'] },
        title: 'Bestellingen Beheren - SSSKL'
      },
      {
        path: 'announcements',
        loadComponent: () => import('./announcements/announcements.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        title: 'Mededelingen Beheren - SSSKL'
      },
      {
        path: 'users',
        loadComponent: () => import('./users/users.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        title: 'Gebruikers Beheren - SSSKL'
      },
      {
        path: 'achievements',
        loadComponent: () => import('./achievements/achievements.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        title: 'Achievements Beheren - SSSKL'
      },
      {
        path: 'roles',
        loadComponent: () => import('./roles/roles.component').then(m => m.RolesComponent),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        title: 'Rollen Beheren - SSSKL'
      }
    ]
  }
];

export default routes;
