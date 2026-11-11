import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';
import { MessageService, ConfirmationService } from 'primeng/api';
import { Toast } from 'primeng/toast';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { EmbeddedService } from '../../core/services/embedded.service';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, SidebarComponent, HeaderComponent, Toast, ConfirmDialog],
  template: `
    <div class="layout-container bg-surface-50 dark:bg-surface-950" [class.embedded]="embedded">
      <p-toast [position]="embedded ? 'bottom-center' : 'bottom-left'" />
      <p-confirm-dialog />
      <!-- In the mobile app the native tab bar and "Meer" page replace the header and menu. -->
      @if (!embedded) {
        <app-sidebar [isOpen]="isSidebarOpen()" (close)="isSidebarOpen.set(false)" />
      }
      <div class="main-wrapper">
        @if (!embedded) {
          <app-header [isSidebarOpen]="isSidebarOpen()" (menuToggled)="toggleSidebar()" />
        }
        <main class="content-area">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: `
    .layout-container {
      display: flex;
      height: 100vh;
      overflow: hidden;
      position: relative;
    }
    .main-wrapper {
      flex: 1;
      display: flex;
      flex-direction: column;
      min-width: 0; /* Prevents flex flex-child from overflowing */
    }
    .content-area {
      flex: 1;
      padding: 2rem;
      overflow-y: auto;

      & ::ng-deep > * {
        display: block;
        animation: fadeIn 0.3s ease-out;
      }
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(10px); }
      to { opacity: 1; transform: translateY(0); }
    }
    @media (max-width: 768px) {
      .content-area {
        padding: 1rem;
      }
    }
    /* Mobile app: no browser chrome, so behave like a native scroll view. */
    .embedded {
      height: 100dvh;
    }
    .embedded .content-area {
      padding: 0.5rem 1rem calc(1.5rem + env(safe-area-inset-bottom));
      overscroll-behavior-y: contain;
      -webkit-overflow-scrolling: touch;
      -webkit-tap-highlight-color: transparent;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayoutComponent {
  protected readonly embedded = inject(EmbeddedService).isEmbedded;
  isSidebarOpen = signal(false);

  toggleSidebar() {
    this.isSidebarOpen.update(open => !open);
  }
}
