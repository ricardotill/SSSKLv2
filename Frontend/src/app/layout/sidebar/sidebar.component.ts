import { Component, ChangeDetectionStrategy, input, output, inject, computed } from '@angular/core';
import { MenuModule } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { CommonModule } from '@angular/common';
import { AvatarModule } from 'primeng/avatar';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { BrandingComponent } from '../../shared/components/branding/branding.component';

import { ResolveApiUrlPipe } from '../../shared/pipes/resolve-api-url.pipe';
import { UserProfileDrawerService } from '../../core/services/user-profile-drawer.service';

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, MenuModule, AvatarModule, RouterModule, BrandingComponent, ResolveApiUrlPipe],
  template: `
    @if (isOpen()) {
      <div class="mobile-overlay" (click)="close.emit()"></div>
    }
    
    <aside class="sidebar" [class.sidebar-open]="isOpen()">
      <div class="logo">
        <app-branding />
      </div>
      <div class="flex-1 overflow-y-auto min-h-0 custom-scrollbar">
        <p-menu [model]="items()" styleClass="w-full border-none bg-transparent" (click)="close.emit()" />
      </div>
      
      <div class="pb-4 flex flex-col">
        @if (authService.isAuthenticated()) {
          <div class="flex items-center gap-3 px-6 py-3 cursor-pointer text-surface-200 hover:text-white hover:bg-surface-700 transition-colors" (click)="openOwnProfile()">
            <p-avatar 
              [image]="(authService.currentUser()?.profilePictureUrl | resolveApiUrl) || undefined" 
              [label]="!authService.currentUser()?.profilePictureUrl ? authService.currentUser()?.fullName?.substring(0,1) : undefined"
              shape="circle" 
              styleClass="w-8 h-8 flex-shrink-0"
            ></p-avatar>
            <span class="font-medium truncate">{{ authService.currentUser()?.fullName ?? authService.currentUser()?.userName }}</span>
          </div>
          
          <a class="flex items-center gap-3 px-6 py-3 cursor-pointer text-red-500 hover:text-red-400 hover:bg-surface-700 transition-colors" (click)="authService.logout(); close.emit()" style="text-decoration: none;">
            <div class="w-8 h-8 flex items-center justify-center">
              <i class="pi pi-sign-out text-lg"></i>
            </div>
            <span class="font-medium">{{ ls.t().logout }}</span>
          </a>
        } @else {
          <a class="flex items-center gap-3 px-6 py-3 cursor-pointer hover:bg-surface-700 transition-colors" [routerLink]="['/login']" [queryParams]="{ returnUrl: router.url }" style="text-decoration: none; color: var(--p-primary-color, #3b82f6);" (click)="close.emit()">
            <div class="w-8 h-8 flex items-center justify-center">
              <i class="pi pi-sign-in text-lg"></i>
            </div>
            <span class="font-medium">{{ ls.t().login }}</span>
          </a>
          
          <a class="flex items-center gap-3 px-6 py-3 cursor-pointer text-surface-200 hover:text-white hover:bg-surface-700 transition-colors" [routerLink]="['/register']" style="text-decoration: none;" (click)="close.emit()">
            <div class="w-8 h-8 flex items-center justify-center">
              <i class="pi pi-user-plus text-lg"></i>
            </div>
            <span class="font-medium">{{ ls.t().register }}</span>
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
      overflow: hidden;
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
      height: auto !important;
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
    
    .custom-scrollbar {
      scrollbar-width: thin;
      scrollbar-color: var(--p-surface-600) transparent;
    }
    .custom-scrollbar::-webkit-scrollbar {
      width: 4px;
    }
    .custom-scrollbar::-webkit-scrollbar-track {
      background: transparent;
    }
    .custom-scrollbar::-webkit-scrollbar-thumb {
      background: var(--p-surface-600, #475569);
      border-radius: 10px;
    }
    .custom-scrollbar::-webkit-scrollbar-thumb:hover {
      background: var(--p-primary-500, #3b82f6);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarComponent {
  authService = inject(AuthService);
  ls = inject(LanguageService);
  private readonly drawerService = inject(UserProfileDrawerService);
  public router = inject(Router);
  isOpen = input<boolean>(false);
  close = output<void>();

  openOwnProfile() {
    const user = this.authService.currentUser();
    if (user) {
      this.drawerService.open(user.id);
      this.close.emit();
    }
  }
  items = computed<MenuItem[]>(() => {
    const t = this.ls.t();
    const baseItems: MenuItem[] = [];

    if (!this.authService.isAuthenticated()) {
      baseItems.push({
        label: t.main,
        items: [
          { label: t.homepage, icon: 'pi pi-home', routerLink: '/', routerLinkActiveOptions: { exact: true } },
          { label: t.leaderboard, icon: 'pi pi-chart-bar', routerLink: '/leaderboard' }
        ]
      });
    }

    if (this.authService.isAuthenticated()) {
      baseItems.push(
        {
          label: t.general,
          items: [
            { label: t.order, icon: 'pi pi-shopping-cart', routerLink: '/pos' },
            { label: t.events, icon: 'pi pi-calendar', routerLink: '/events' },
            { label: t.my_orders, icon: 'pi pi-history', routerLink: '/orders/personal' },
            { label: t.my_saldo, icon: 'pi pi-wallet', routerLink: '/orders/saldo' },
            { label: t.achievements, icon: 'pi pi-verified', routerLink: '/achievements' },
            { label: t.leaderboard, icon: 'pi pi-chart-bar', routerLink: '/leaderboard' },
            { label: t.user_overview, icon: 'pi pi-users', routerLink: '/users' },
            { label: t.about, icon: 'pi pi-info-circle', routerLink: '/about' },
            { label: t.settings, icon: 'pi pi-cog', routerLink: '/settings' }
          ]
        }
      );

      const currentUser = this.authService.currentUser();
      if (currentUser) {
        const roles = currentUser.roles || [];
        const isAdmin = roles.includes('Admin');
        const isKiosk = roles.includes('Kiosk');

        const adminItems: MenuItem[] = [];

        if (isAdmin) {
          adminItems.push(
            { label: t.users, icon: 'pi pi-users', routerLink: '/admin/users' },
            { label: t.roles, icon: 'pi pi-id-card', routerLink: '/admin/roles' },
            { label: t.products, icon: 'pi pi-box', routerLink: '/admin/products' },
            { label: t.achievements, icon: 'pi pi-verified', routerLink: '/admin/achievements' },
            { label: t.announcements, icon: 'pi pi-megaphone', routerLink: '/admin/announcements' },
            { label: t.global_settings, icon: 'pi pi-info-circle', routerLink: '/admin/global-settings' },
            { label: t.top_ups, icon: 'pi pi-wallet', routerLink: '/admin/topups' }
          );
        }

        if (isAdmin || isKiosk) {
          adminItems.push(
            { label: t.orders, icon: 'pi pi-list', routerLink: '/admin/orders' }
          );
        }

        if (adminItems.length > 0) {
          baseItems.push({
            label: t.admin,
            items: adminItems
          });
        }
      }
    }

    return baseItems;
  });
}
