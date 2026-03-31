import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CardModule } from 'primeng/card';
import { DataViewModule, DataViewLazyLoadEvent } from 'primeng/dataview';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { EventService } from './services/event.service';
import { LanguageService } from '../../core/services/language.service';
import { AuthService } from '../../core/auth/auth.service';
import { EventDto } from '../../core/models/event.model';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { FormsModule } from '@angular/forms';
import { AvatarModule } from 'primeng/avatar';

@Component({
  selector: 'app-events',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    CardModule,
    DataViewModule,
    ButtonModule,
    TagModule,
    ProgressSpinnerModule,
    ToggleButtonModule,
    FormsModule,
    AvatarModule
  ],
  template: `
    <div class="flex flex-col gap-6">
      <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h1 class="text-3xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().events }}</h1>
          <p class="text-surface-500 m-0">{{ ls.t().no_events_desc }}</p>
        </div>
        <div class="flex items-center gap-3">
          <p-toggleButton 
            [(ngModel)]="futureOnly" 
            onLabel="Toekomstig" 
            offLabel="Alle" 
            onIcon="pi pi-calendar" 
            offIcon="pi pi-calendar-plus" 
            (onChange)="loadEvents()"
          />
          @if (canCreate()) {
            <p-button 
              [label]="ls.t().create_event" 
              icon="pi pi-plus" 
              [routerLink]="['/events/new']" 
              severity="primary" 
            />
          }
        </div>
      </div>

      <p-dataView 
        [value]="events()" 
        layout="grid" 
        [paginator]="true" 
        paginatorStyleClass="mt-6"
        [rows]="rows" 
        [totalRecords]="totalRecords()"
        [lazy]="true"
        (onLazyLoad)="onLazyLoad($event)"
        [loading]="loading()"
      >
        <ng-template #grid let-items>
          <div class="grid grid-cols-12 gap-6">
            @for (event of items; track event.id) {
              <div class="col-span-12 sm:col-span-6 lg:col-span-4 xl:col-span-3">
                <p-card 
                  [styleClass]="'h-full overflow-hidden hover:shadow-lg transition-all duration-300 border border-surface-200 dark:border-surface-700 cursor-pointer ' + (isPassed(event.endDateTime) ? 'opacity-60 grayscale-[0.5]' : '')"
                  [routerLink]="['/events', event.id]"
                >
                  <ng-template pTemplate="header">
                    <div class="relative h-48 w-full bg-surface-100 dark:bg-surface-800 flex items-center justify-center">
                      @if (event.imageUrl) {
                        <img [src]="event.imageUrl" alt="Event Image" class="w-full h-full object-cover">
                      } @else {
                        <i class="pi pi-calendar text-5xl text-surface-300 dark:text-surface-600"></i>
                      }
                      <div class="absolute top-3 left-3">
                        @if (isPassed(event.endDateTime)) {
                          <p-tag severity="secondary" icon="pi pi-history" [value]="ls.t().past"></p-tag>
                        }
                      </div>
                      <div class="absolute top-3 right-3">
                        @if (event.userResponse === 'Accepted') {
                          <p-tag severity="success" icon="pi pi-check" value="Aangemeld"></p-tag>
                        } @else if (event.userResponse === 'Declined') {
                          <p-tag severity="danger" icon="pi pi-times" value="Afgemeld"></p-tag>
                        }
                      </div>
                    </div>
                  </ng-template>

                  <div class="flex flex-col gap-3">
                    <h2 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0 line-clamp-1">
                      {{ event.title }}
                    </h2>
                    
                    <div class="flex items-center gap-2 text-surface-500 text-sm">
                      <i class="pi pi-clock"></i>
                      <span>{{ event.startDateTime | date:'d MMM yyyy, HH:mm' }}</span>
                    </div>

                    <div class="text-surface-600 dark:text-surface-400 text-sm line-clamp-2" [innerHTML]="event.description">
                    </div>

                    <div 
                      class="flex mt-2 pt-4 border-t border-surface-200 dark:border-surface-700"
                      [class.justify-between]="event.acceptedUsers.length > 0"
                      [class.justify-center]="event.acceptedUsers.length === 0"
                      [class.items-center]="true"
                    >
                      <div class="flex -space-x-2">
                         @for (user of event.acceptedUsers.slice(0, 3); track user.userId) {
                           <p-avatar 
                              [image]="user.profilePictureUrl || undefined" 
                              [label]="!user.profilePictureUrl ? user.userName?.substring(0,1) : undefined"
                              shape="circle" 
                              size="normal"
                              styleClass="ring-2 ring-surface-0 dark:ring-surface-900"
                              [title]="user.userName"
                           ></p-avatar>
                         }
                         @if (event.acceptedUsers.length > 3) {
                           <div class="flex h-8 w-8 rounded-full ring-2 ring-surface-0 dark:ring-surface-900 bg-surface-200 dark:bg-surface-700 items-center justify-center text-xs font-bold text-surface-600 dark:text-surface-300">
                              +{{ event.acceptedUsers.length - 3 }}
                           </div>
                         }
                      </div>
                      <span class="text-xs text-surface-500">{{ event.acceptedUsers.length }} {{ ls.t().attendees }}</span>
                    </div>
                  </div>
                </p-card>
              </div>
            }
          </div>
        </ng-template>

        <ng-template #emptymessage>
          <div class="flex flex-col items-center justify-center p-12 text-surface-500 bg-surface-50 dark:bg-surface-800/50 rounded-xl border border-dashed border-surface-300 dark:border-surface-600 w-full">
            <i class="pi pi-calendar-times text-6xl mb-4 opacity-20"></i>
            <p class="text-lg font-medium">{{ ls.t().no_events }}</p>
          </div>
        </ng-template>
      </p-dataView>
    </div>
  `,
  styles: [`
    :host ::ng-deep .p-dataview-content {
      background: transparent !important;
      border: none !important;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class EventsComponent implements OnInit {
  private readonly eventService = inject(EventService);
  private readonly authService = inject(AuthService);
  ls = inject(LanguageService);

  events = signal<EventDto[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  canCreate = computed(() => {
    const user = this.authService.currentUser();
    if (!user) return false;
    return user.roles.includes('Admin') || user.roles.includes('User');
  });

  rows = 12;
  first = 0;
  futureOnly = true;

  constructor() {}

  isPassed(date?: string): boolean {
    if (!date) return false;
    return new Date(date) < new Date();
  }

  ngOnInit(): void {
    // Initial load will be triggered by DataView onLazyLoad
  }

  onLazyLoad(event: DataViewLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.rows = event.rows ?? 12;
    this.loadEvents();
  }

  loadEvents(): void {
    this.loading.set(true);
    this.eventService.getEvents(this.first, this.rows, this.futureOnly).subscribe({
      next: (response) => {
        this.events.set(response.items);
        this.totalRecords.set(response.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
