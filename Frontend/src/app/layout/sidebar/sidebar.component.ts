import { Component, ChangeDetectionStrategy, ViewEncapsulation, input, output, inject, computed } from '@angular/core';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { CommonModule } from '@angular/common';
import { TagModule } from 'primeng/tag';
import { AvatarModule } from 'primeng/avatar';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, MenuModule, TagModule, AvatarModule, RouterModule],
  template: `
    @if (isOpen()) {
      <div class="mobile-overlay" (click)="close.emit()"></div>
    }
    
    <aside class="sidebar" [class.sidebar-open]="isOpen()">
      <div class="logo">
        <h2 class="text-xl font-bold m-0">SSSKL</h2> <p-tag class="ml-2" value="v2" />
      </div>
      <p-menu [model]="items()" styleClass="w-full border-none bg-transparent flex-1" />
      
      <div class="mt-auto pb-4 flex flex-col">
        @if (authService.isAuthenticated()) {
          <a class="flex items-center gap-3 px-6 py-3 cursor-pointer text-surface-200 hover:text-white hover:bg-surface-700 transition-colors" [routerLink]="['/settings']" style="text-decoration: none;">
            @if (authService.currentUser()?.profilePictureBase64) {
              <p-avatar [image]="'data:image/jpeg;base64,' + authService.currentUser()?.profilePictureBase64" shape="circle" styleClass="w-8 h-8" />
            } @else {
              <p-avatar icon="pi pi-user" shape="circle" styleClass="w-8 h-8 bg-surface-700 text-surface-200" />
            }
            <span class="font-medium truncate">{{ authService.currentUser()?.fullName ?? authService.currentUser()?.userName }}</span>
          </a>
          
          <a class="flex items-center gap-3 px-6 py-3 cursor-pointer text-red-500 hover:text-red-400 hover:bg-surface-700 transition-colors" (click)="authService.logout()" style="text-decoration: none;">
            <div class="w-8 h-8 flex items-center justify-center">
              <i class="pi pi-sign-out text-lg"></i>
            </div>
            <span class="font-medium">Logout</span>
          </a>
        } @else {
          <a class="flex items-center gap-3 px-6 py-3 cursor-pointer hover:bg-surface-700 transition-colors" [routerLink]="['/login']" style="text-decoration: none; color: var(--p-primary-color, #3b82f6);">
            <div class="w-8 h-8 flex items-center justify-center">
              <i class="pi pi-sign-in text-lg"></i>
            </div>
            <span class="font-medium">Login</span>
          </a>
          
          <a class="flex items-center gap-3 px-6 py-3 cursor-pointer text-surface-200 hover:text-white hover:bg-surface-700 transition-colors" [routerLink]="['/register']" style="text-decoration: none;">
            <div class="w-8 h-8 flex items-center justify-center">
              <i class="pi pi-user-plus text-lg"></i>
            </div>
            <span class="font-medium">Register</span>
          </a>
        }
      </div>
    </aside>
  `,
  styles: `
    .sidebar {
      width: 250px;
      height: 100vh;
      background-color: var(--p-surface-900, #1e293b);
      color: white;
      display: flex;
      flex-direction: column;
      flex-shrink: 0;
      transition: transform 0.3s ease;
      z-index: 1000;
    }
    .logo {
      height: 64px;
      display: flex;
      align-items: center;
      padding: 0 1.5rem;
      border-bottom: 1px solid var(--p-surface-700, #334155);
      box-sizing: border-box;
    }
    .mobile-overlay {
      display: none;
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      z-index: 999;
    }
    ::ng-deep .p-menu {
      background: transparent !important;
      border: 0 !important;
      padding: 1rem 0;
    }
    ::ng-deep .p-menu .p-menu-item-link {
      background: transparent !important;
      color: var(--p-surface-200, #cbd5e1) !important;
      padding: 0.75rem 1.5rem;
    }
    ::ng-deep .p-menu .p-menu-item-link:hover {
      background: var(--p-surface-700, #334155) !important;
      color: white !important;
    }
    ::ng-deep .p-menu .p-menu-item-icon {
      color: var(--p-surface-200, #cbd5e1) !important;
    }
    ::ng-deep .p-menu .p-menu-item-link.p-menu-item-link-active,
    ::ng-deep .p-menu .p-menu-item-link.router-link-active,
    ::ng-deep .p-menu .p-highlight > .p-menu-item-link {
      background: var(--p-surface-800, #1e293b) !important;
      color: var(--p-primary-color, #3b82f6) !important;
      border-right: 3px solid var(--p-primary-color, #3b82f6);
    }
    ::ng-deep .p-menu .p-menu-item-link.p-menu-item-link-active .p-menu-item-icon,
    ::ng-deep .p-menu .p-menu-item-link.router-link-active .p-menu-item-icon,
    ::ng-deep .p-menu .p-highlight > .p-menu-item-link .p-menu-item-icon {
      color: var(--p-primary-color, #3b82f6) !important;
    }

    @media (max-width: 768px) {
      .sidebar {
        position: fixed;
        left: 0;
        top: 0;
        transform: translateX(-100%);
      }
      .sidebar.sidebar-open {
        transform: translateX(0);
      }
      .mobile-overlay {
        display: block;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarComponent {
  authService = inject(AuthService);
  isOpen = input<boolean>(false);
  close = output<void>();
  items = computed<MenuItem[]>(() => {
    const baseItems: MenuItem[] = [
      { label: 'Homepage', icon: 'pi pi-home', routerLink: '/', routerLinkActiveOptions: { exact: true } }
    ];

    if (this.authService.isAuthenticated()) {
      baseItems.push(
        { label: 'Bestellen', icon: 'pi pi-shopping-cart', routerLink: '/pos' },
        { label: 'My Orders', icon: 'pi pi-history', routerLink: '/orders/personal' },
        { label: 'Users', icon: 'pi pi-users', routerLink: '/users' },
        { label: 'Products', icon: 'pi pi-box', routerLink: '/products' },
        { label: 'Announcements', icon: 'pi pi-bullhorn', routerLink: '/announcements' },
        { label: 'Achievements', icon: 'pi pi-star', routerLink: '/achievements' },
        { label: 'Top-Ups', icon: 'pi pi-wallet', routerLink: '/topups' },
        { label: 'Settings', icon: 'pi pi-cog', routerLink: '/settings' }
      );
    }

    return baseItems;
  });
}
