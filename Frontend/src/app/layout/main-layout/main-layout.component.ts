import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, SidebarComponent, HeaderComponent, ToastModule],
  template: `
    <div class="layout-container bg-surface-50 dark:bg-surface-950">
      <p-toast position="bottom-left" />
      <app-sidebar [isOpen]="isSidebarOpen()" (close)="isSidebarOpen.set(false)" />
      <div class="main-wrapper">
        <app-header [isSidebarOpen]="isSidebarOpen()" (menuToggled)="toggleSidebar()" />
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
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class MainLayoutComponent {
  isSidebarOpen = signal(false);

  toggleSidebar() {
    this.isSidebarOpen.update(open => !open);
  }
}
