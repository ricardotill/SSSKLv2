import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { NotificationService } from '../../core/services/notification.service';
import { LanguageService } from '../../core/services/language.service';
import { NotificationDto } from '../../core/models/notification.model';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { PaginatorModule } from 'primeng/paginator';
import { Router } from '@angular/router';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, ButtonModule, ProgressSpinnerModule, PaginatorModule],
  providers: [DatePipe],
  template: `
    <div class="max-w-4xl mx-auto py-8 px-4">
      <div class="flex justify-between items-center mb-8">
         <div>
            <h1 class="text-3xl font-black tracking-tight text-surface-900 dark:text-surface-0 m-0">Notificaties</h1>
            <p class="text-surface-500 mt-2 m-0">Bekijk je recente activiteit en reacties</p>
         </div>
         @if (notificationService.unreadCount() > 0) {
            <p-button label="Alles gelezen" icon="pi pi-check-circle" size="small" (onClick)="markAllAsRead()" [loading]="markingAll()" />
         }
      </div>

      <div class="bg-surface-0 dark:bg-surface-900 rounded-3xl shadow-sm border border-surface-200 dark:border-surface-800 overflow-hidden">
        @if (loading() && notifications().length === 0) {
          <div class="flex justify-center p-12">
            <p-progressSpinner styleClass="w-10 h-10" />
          </div>
        } @else if (notifications().length === 0) {
           <div class="flex flex-col items-center justify-center p-16 text-surface-500">
             <i class="pi pi-bell-slash text-6xl mb-6 text-surface-300 dark:text-surface-600"></i>
             <h3 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0">Nog geen notificaties</h3>
             <p class="mt-2">Wanneer je notificaties krijgt, verschijnen ze hier.</p>
           </div>
        } @else {
          <div class="flex flex-col divide-y divide-surface-100 dark:divide-surface-800">
            @for (notification of notifications(); track notification.id) {
              <div class="flex items-start gap-4 p-6 transition-colors hover:bg-surface-50 dark:hover:bg-surface-800/50 cursor-pointer"
                   [ngClass]="{'bg-primary-50 dark:bg-primary-900/10': !notification.isRead}"
                   (click)="onNotificationClick(notification)">
                 <div class="w-12 h-12 flex items-center justify-center rounded-full flex-shrink-0" [ngClass]="notification.isRead ? 'bg-surface-100 dark:bg-surface-800 text-surface-500' : 'bg-primary-100 dark:bg-primary-900/30 text-primary-600 dark:text-primary-400'">
                    <i class="pi text-xl" [ngClass]="notification.isRead ? 'pi-envelope' : 'pi-bell'"></i>
                 </div>
                 <div class="flex-1 min-w-0">
                    <div class="flex justify-between items-start gap-4 mb-1">
                        <h4 class="m-0 font-bold" [ngClass]="{'text-surface-900 dark:text-surface-0': !notification.isRead, 'text-surface-700 dark:text-surface-300': notification.isRead}">
                          {{ notification.title }}
                        </h4>
                        <span class="text-sm font-medium text-surface-500 whitespace-nowrap">{{ datePipe.transform(notification.createdOn, 'medium') }}</span>
                    </div>
                    <p class="m-0 text-surface-600 dark:text-surface-400 leading-relaxed">{{ notification.message }}</p>
                 </div>
              </div>
            }
          </div>

          @if (totalRecords() > pageSize) {
             <div class="p-4 border-t border-surface-200 dark:border-surface-800 bg-surface-50 dark:bg-surface-900/50">
               <p-paginator (onPageChange)="onPageChange($event)" [first]="skip()" [rows]="pageSize" [totalRecords]="totalRecords()" />
             </div>
          }
        }
      </div>
      <div class="flex justify-between mt-4">
          <p-button icon="pi pi-arrow-left" label="Vorige" [disabled]="skip() === 0" (onClick)="prevPage()" [text]="true" />
          <p-button icon="pi pi-arrow-right" label="Volgende" iconPos="right" [disabled]="notifications().length < pageSize" (onClick)="nextPage()" [text]="true" />
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsComponent implements OnInit {
  notificationService = inject(NotificationService);
  ls = inject(LanguageService);
  private router = inject(Router);
  public datePipe = inject(DatePipe);

  notifications = signal<NotificationDto[]>([]);
  loading = signal(false);
  markingAll = signal(false);

  skip = signal(0);
  pageSize = 20;

  // We might not get totalRecords from backend. I will use skip/take logic 
  // and manually implement next/prev if total count is missing.
  totalRecords = signal(0); // Optional placeholder

  ngOnInit() {
    this.loadNotifications();
  }

  loadNotifications() {
    this.loading.set(true);
    // unreadOnly is false, because we want to see all
    this.notificationService.getNotifications(false, this.skip(), this.pageSize).subscribe({
      next: (data) => {
        this.notifications.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  nextPage() {
    this.skip.update(s => s + this.pageSize);
    this.loadNotifications();
  }

  prevPage() {
    if (this.skip() > 0) {
      this.skip.update(s => s - this.pageSize);
      this.loadNotifications();
    }
  }

  onPageChange(event: any) {
    this.skip.set(event.first);
    this.loadNotifications();
  }

  onNotificationClick(notification: NotificationDto) {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe(() => {
        this.notifications.update(list => list.map(n => n.id === notification.id ? { ...n, isRead: true } : n));
      });
    }

    if (notification.linkUri) {
      this.router.navigateByUrl(notification.linkUri);
    }
  }

  markAllAsRead() {
    this.markingAll.set(true);
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
        this.markingAll.set(false);
      },
      error: () => this.markingAll.set(false)
    });
  }
}
