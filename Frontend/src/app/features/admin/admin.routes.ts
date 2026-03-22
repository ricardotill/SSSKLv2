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
        title: 'Products - Admin'
      },
      {
        path: 'topups',
        loadComponent: () => import('./topups/topups.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        title: 'Top-Ups - Admin'
      },
      {
        path: 'orders',
        loadComponent: () => import('./orders/orders.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin', 'Kiosk'] },
        title: 'Orders - Admin'
      },
      {
        path: 'announcements',
        loadComponent: () => import('./announcements/announcements.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        title: 'Announcements - Admin'
      },
      {
        path: 'users',
        loadComponent: () => import('./users/users.component'),
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        title: 'Users - Admin'
      }
    ]
  }
];

export default routes;
