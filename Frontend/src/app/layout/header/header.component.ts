import { Component, ChangeDetectionStrategy, output, input, inject } from '@angular/core';
import { ToolbarModule } from 'primeng/toolbar';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/services/language.service';

@Component({
  selector: 'app-header',
  imports: [CommonModule, ToolbarModule, AvatarModule, ButtonModule, RouterLink],
  template: `
    <header class="header">
      <p-toolbar styleClass="bg-surface-0 dark:bg-surface-900 border-none border-b border-surface-200 dark:border-surface-700 w-full mb-0 h-16 px-4 relative">
        <ng-template pTemplate="start">
          <div class="flex items-center gap-3">
              <p-button icon="pi pi-bars" (onClick)="menuToggled.emit()" [text]="true" severity="secondary" styleClass="md:!hidden" />
            <h1 class="text-lg font-bold m-0 text-surface-900 dark:text-surface-0">
              {{ ls.t().welcome }}{{ authService.currentUser()?.name ? ' ' + authService.currentUser()?.name : '' }}!
            </h1>
          </div>
        </ng-template>

        <ng-template pTemplate="end">
          <div class="flex items-center gap-4">
            @if (authService.isAuthenticated()) {
              <div class="flex items-center gap-2 bg-surface-100 dark:bg-surface-800 px-3 py-1.5 rounded-full border border-surface-200 dark:border-surface-700">
                <i class="pi pi-wallet text-primary-500"></i>
                <span class="font-semibold text-surface-900 dark:text-surface-0">{{ (authService.currentUser()?.saldo || 0) | currency:'EUR':'symbol' }}</span>
              </div>
            } @else {
              <p-button [label]="ls.t().login" icon="pi pi-sign-in" severity="primary" routerLink="/login" />
            }
          </div>
        </ng-template>
      </p-toolbar>
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
export class HeaderComponent {
  authService = inject(AuthService);
  ls = inject(LanguageService);
  private router = inject(Router);


  isSidebarOpen = input<boolean>(false);
  menuToggled = output<void>();
}
