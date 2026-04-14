import { Component, ChangeDetectionStrategy, inject, signal, effect, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { Router } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { LanguageService } from '../../../core/services/language.service';
import { NotificationDto } from '../../../core/models/notification.model';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-notification-drawer',
  standalone: true,
  imports: [CommonModule, DrawerModule, ButtonModule, ProgressSpinnerModule],
  providers: [DatePipe],
  template: `
    <p-drawer [(visible)]="isOpen" position="top" [showCloseIcon]="true" styleClass="notification-drawer !h-auto w-full flex flex-col bg-surface-0 dark:bg-surface-900 border-none rounded-b-2xl shadow-xl max-h-[90vh]">
      <ng-template pTemplate="header">
        <div class="flex items-center justify-between w-full pr-2">
          <h2 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0">Notificaties</h2>
          @if (notifications().length > 0) {
            <p-button label="Alles gelezen" [text]="true" size="small" (onClick)="markAllAsRead()" [loading]="markingAll()" styleClass="text-sm px-2 py-1" />
          }
        </div>
      </ng-template>

      <div class="px-2 pb-2">
        @if (loading()) {
          <div class="flex justify-center p-8">
            <p-progressSpinner styleClass="w-8 h-8" />
          </div>
        } @else if (notifications().length === 0) {
           <div class="flex flex-col items-center justify-center p-8 text-surface-500">
             <i class="pi pi-bell-slash text-4xl mb-4"></i>
             <p>Geen nieuwe notificaties</p>
           </div>
        } @else {
          <div class="flex flex-col gap-2">
            @for (notification of notifications(); track notification.id) {
              <div class="group flex items-center gap-4 p-4 rounded-xl hover:bg-surface-100 dark:hover:bg-surface-800 transition-colors border border-transparent hover:border-surface-200 dark:hover:border-surface-700 cursor-pointer"
                   (click)="onNotificationClick(notification)">
                 <div class="bg-primary-100 dark:bg-primary-900/30 w-11 h-11 flex items-center justify-center rounded-full text-primary-600 dark:text-primary-400 flex-shrink-0">
                    <i class="pi pi-bell text-lg"></i>
                 </div>
                 <div class="flex-1 min-w-0">
                    <h4 class="m-0 font-bold text-surface-900 dark:text-surface-0 truncate text-base leading-tight">{{ notification.title }}</h4>
                    <p class="m-0 mt-0.5 text-sm text-surface-600 dark:text-surface-400 line-clamp-2 leading-snug">{{ notification.message }}</p>
                 </div>
                 <div class="flex items-center gap-2 flex-shrink-0 ml-2">
                    <span class="text-xs font-medium text-surface-500 whitespace-nowrap">{{ datePipe.transform(notification.createdOn, 'shortTime') }}</span>
                    <p-button icon="pi pi-check" [rounded]="true" [outlined]="true" size="small" (onClick)="$event.stopPropagation(); markAsRead(notification)" title="Gelezen markeren" styleClass="w-8 h-8" />
                 </div>
              </div>
            }
          </div>
        }

        <div class="mt-4">
          @if (notificationService.unreadCount() > notifications().length) {
            <p-button 
              label="Bekijk alle {{ notificationService.unreadCount() }} notificaties" 
              severity="primary" 
              styleClass="w-full"
              (onClick)="viewAll()" />
          } @else {
            <p-button 
              label="Bekijk oude notificaties" 
              [outlined]="true" 
              styleClass="w-full"
              (onClick)="viewAll()" />
          }
        </div>
      </div>
    </p-drawer>
  `,
  styles: `
    ::ng-deep .notification-drawer {
      height: auto !important;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationDrawerComponent {
  public notificationService = inject(NotificationService);
  public ls = inject(LanguageService);
  private router = inject(Router);
  public datePipe = inject(DatePipe);

  visible = input<boolean>(false);
  visibleChange = output<boolean>();

  notifications = signal<NotificationDto[]>([]);
  loading = signal(false);
  markingAll = signal(false);

  set isOpen(val: boolean) {
    this.visibleChange.emit(val);
  }
  get isOpen() {
    return this.visible();
  }

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.loadNotifications();
      }
    }, { allowSignalWrites: true });
  }

  loadNotifications() {
    this.loading.set(true);
    this.notificationService.getNotifications(true, 0, 6).pipe(
      finalize(() => this.loading.set(false))
    ).subscribe(data => {
      this.notifications.set(data);
    });
  }

  markAsRead(notification: NotificationDto) {
    this.notificationService.markAsRead(notification.id).subscribe(() => {
      this.notifications.update(list => list.filter(n => n.id !== notification.id));
    });
  }

  markAllAsRead() {
    this.markingAll.set(true);
    this.notificationService.markAllAsRead().pipe(
      finalize(() => this.markingAll.set(false))
    ).subscribe(() => {
      this.notifications.set([]);
    });
  }

  onNotificationClick(notification: NotificationDto) {
    this.markAsRead(notification);
    if (notification.linkUri) {
      this.isOpen = false;
      this.router.navigateByUrl(notification.linkUri);
    }
  }

  viewAll() {
    this.isOpen = false;
    this.router.navigate(['/notifications']);
  }
}
