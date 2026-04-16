import { Component, ChangeDetectionStrategy, output, input, inject, OnInit } from '@angular/core';
import { ToolbarModule } from 'primeng/toolbar';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { CommonModule } from '@angular/common';
import { RouterLink, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationDrawerComponent } from '../../shared/components/notification-drawer/notification-drawer.component';
import { BrandingComponent } from '../../shared/components/branding/branding.component';

@Component({
  selector: 'app-header',
  imports: [CommonModule, ToolbarModule, AvatarModule, ButtonModule, BadgeModule, RouterLink, NotificationDrawerComponent, BrandingComponent],
  template: `
    <header class="header">
      <p-toolbar styleClass="bg-surface-0 dark:bg-surface-900 border-none border-b border-surface-200 dark:border-surface-700 w-full mb-0 h-16 px-4 relative">
        <ng-template pTemplate="start">
          <div class="flex items-center gap-2">
              <p-button icon="pi pi-bars" (onClick)="menuToggled.emit()" [text]="true" severity="secondary" styleClass="md:!hidden flex-shrink-0" />
              <app-branding [showVersion]="false" class="scale-[0.85] sm:scale-100 origin-left md:!hidden" />
          </div>
        </ng-template>

        <ng-template pTemplate="end">
          <div class="flex items-center gap-4">
            @if (authService.isAuthenticated()) {
              <p-button 
                icon="pi pi-bell" 
                [text]="true" 
                [rounded]="true" 
                severity="secondary" 
                (onClick)="isNotificationDrawerOpen = true"
                styleClass="!overflow-visible relative text-surface-600 dark:text-surface-400 hover:bg-surface-100 dark:hover:bg-surface-800 transition-colors">
                @if (notificationService.unreadCount() > 0) {
                  <p-badge 
                    [value]="notificationService.unreadCount() > 9 ? '9+' : notificationService.unreadCount()" 
                    severity="danger" 
                    styleClass="absolute p-0 min-w-[18px] h-[18px] leading-[18px] text-[10px] -top-1 -right-1 flex items-center justify-center m-0" />
                }
              </p-button>

              <div 
                routerLink="/orders/saldo"
                class="flex items-center gap-2 bg-surface-100 dark:bg-surface-800 px-3 py-1.5 rounded-full border border-surface-200 dark:border-surface-700 cursor-pointer hover:bg-surface-200 dark:hover:bg-surface-700 transition-colors duration-200"
              >
                <i class="pi pi-wallet text-primary-500"></i>
                <span class="font-semibold text-surface-900 dark:text-surface-0">{{ (authService.currentUser()?.saldo || 0) | currency:'EUR':'symbol' }}</span>
              </div>
            } @else {
              <p-button [label]="ls.t().login" icon="pi pi-sign-in" severity="primary" [routerLink]="['/login']" [queryParams]="{ returnUrl: router.url }" />
            }
          </div>
        </ng-template>
      </p-toolbar>

      <app-notification-drawer [(visible)]="isNotificationDrawerOpen" />
    </header>
  `,
  styles: `
    .header {
      width: 100%;
    }
    ::ng-deep .p-toolbar {
      border-radius: 0 !important;
      border-top: none !important;
      border-left: none !important;
      border-right: none !important;
      padding: 0 1.5rem !important;
      height: 64px !important;
    }

  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent implements OnInit {
  authService = inject(AuthService);
  ls = inject(LanguageService);
  notificationService = inject(NotificationService);
  public router = inject(Router);

  isSidebarOpen = input<boolean>(false);
  menuToggled = output<void>();

  isNotificationDrawerOpen = false;



  ngOnInit() {
    if (this.authService.isAuthenticated()) {
      this.notificationService.fetchUnreadCount().subscribe();
    }

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      if (this.authService.isAuthenticated()) {
        this.notificationService.fetchUnreadCount().subscribe();
        this.authService.refreshCurrentUser();
      }
    });
  }
}
