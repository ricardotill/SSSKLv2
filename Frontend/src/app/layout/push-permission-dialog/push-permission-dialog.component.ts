import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { PushNotificationService } from '../../core/services/push-notification.service';
import { LanguageService } from '../../core/services/language.service';

@Component({
  selector: 'app-push-permission-dialog',
  standalone: true,
  imports: [CommonModule, DialogModule, ButtonModule],
  template: `
    <p-dialog 
      [(visible)]="visible" 
      [modal]="true" 
      [closable]="false" 
      [draggable]="false" 
      [resizable]="false"
      [style]="{width: '90vw', maxWidth: '400px'}"
      [showHeader]="false">
      
      <div class="flex flex-col items-center justify-center text-center p-8 w-full">
        <div class="w-20 h-20 flex items-center justify-center bg-primary/10 rounded-full mb-6">
            <i class="pi pi-bell text-4xl text-primary animate-bounce"></i>
        </div>
        
        <h2 class="text-2xl font-bold mb-3 text-surface-900 dark:text-surface-0">
            {{ ls.t()['PUSH.DIALOG.HEADING'] }}
        </h2>
        <p class="text-surface-600 dark:text-surface-400 mb-8 leading-relaxed">
          {{ ls.t()['PUSH.DIALOG.DESCRIPTION'] }}
        </p>

        <div class="flex flex-col w-full gap-3">
            <p-button 
                [label]="ls.t()['PUSH.DIALOG.ENABLE']" 
                icon="pi pi-check" 
                class="w-full"
                [style]="{'width': '100%'}"
                (onClick)="enable()"
                [loading]="loading()">
            </p-button>
            <p-button 
                [label]="ls.t()['PUSH.DIALOG.LATER']" 
                icon="pi pi-clock" 
                class="w-full"
                [style]="{'width': '100%'}"
                severity="secondary"
                [text]="true"
                (onClick)="close()">
            </p-button>
        </div>
      </div>
    </p-dialog>
  `,
  styles: [`
    :host ::ng-deep .p-dialog .p-dialog-header {
      display: none;
    }
    :host ::ng-deep .p-dialog .p-dialog-content {
      border-radius: 12px;
    }
  `]
})
export class PushPermissionDialogComponent {
  private pushService = inject(PushNotificationService);
  protected ls = inject(LanguageService);
  
  visible = true;
  loading = signal(false);

  async enable() {
    this.loading.set(true);
    try {
      await this.pushService.subscribe();
      this.close();
    } catch (err) {
      console.error('Failed to enable push', err);
      // We don't close on error so the user can try again or cancel
    } finally {
      this.loading.set(false);
    }
  }

  close() {
    this.visible = false;
    this.pushService.setPrompted();
  }
}
