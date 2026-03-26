import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { EventService } from '../../../core/services/event.service';
import { LanguageService } from '../../../core/services/language.service';
import { AuthService } from '../../../core/auth/auth.service';
import { EventDto, EventResponseStatus } from '../../../core/models/event.model';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-event-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ButtonModule,
    CardModule,
    TagModule,
    DividerModule,
    ProgressSpinnerModule,
    ToastModule
  ],
  providers: [MessageService],
  template: `
    <div class="max-w-4xl mx-auto">
      <p-toast></p-toast>
      
      @if (loading()) {
        <div class="flex justify-center items-center p-12">
          <p-progressSpinner ariaLabel="loading"></p-progressSpinner>
        </div>
      } @else if (event()) {
        <div class="flex flex-col gap-6">
          <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
            <div class="flex items-center gap-3">
              <p-button icon="pi pi-arrow-left" [text]="true" severity="secondary" routerLink="/events" />
              <h1 class="text-3xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ event()?.title }}</h1>
            </div>
            
            <div class="flex gap-2">
              @if (canEdit()) {
                <p-button [label]="ls.t().edit" icon="pi pi-pencil" severity="secondary" [routerLink]="['/events', event()?.id, 'edit']" />
                <p-button [label]="ls.t().delete" icon="pi pi-trash" severity="danger" (onClick)="deleteEvent()" />
              }
            </div>
          </div>

          <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12 lg:col-span-8 flex flex-col gap-6">
              <p-card styleClass="overflow-hidden">
                <ng-template pTemplate="header">
                  @if (event()?.imageUrl) {
                    <img [src]="event()?.imageUrl" alt="Event Banner" class="w-full h-64 object-cover">
                  }
                </ng-template>
                
                <div class="flex flex-col gap-4">
                  <div class="grid grid-cols-2 gap-4">
                    <div class="flex flex-col gap-1">
                      <span class="text-xs text-surface-500 uppercase font-bold tracking-wider">{{ ls.t().start_date }}</span>
                      <div class="flex items-center gap-2">
                        <i class="pi pi-calendar text-primary-500"></i>
                        <span class="font-medium">{{ event()?.startDateTime | date:'EEEE d MMMM yyyy, HH:mm' }}</span>
                      </div>
                    </div>
                    <div class="flex flex-col gap-1">
                      <span class="text-xs text-surface-500 uppercase font-bold tracking-wider">{{ ls.t().end_date }}</span>
                      <div class="flex items-center gap-2">
                        <i class="pi pi-calendar text-primary-500"></i>
                        <span class="font-medium">{{ event()?.endDateTime | date:'EEEE d MMMM yyyy, HH:mm' }}</span>
                      </div>
                    </div>
                  </div>

                  <p-divider></p-divider>

                  <div class="prose dark:prose-invert max-w-none" [innerHTML]="event()?.description">
                  </div>
                </div>
              </p-card>

              <!-- RSVP Section -->
              <p-card styleClass="bg-primary-50/50 dark:bg-primary-900/10 border-primary-100 dark:border-primary-900/20">
                <div class="flex flex-col md:flex-row justify-between items-center gap-6 p-2">
                  <div class="flex flex-col gap-2 text-center md:text-left">
                    <h3 class="text-xl font-bold m-0 text-surface-900 dark:text-surface-0">Ben je erbij?</h3>
                    <p class="text-surface-600 dark:text-surface-400 m-0">Laat weten of je komt zodat we rekening kunnen houden met de planning.</p>
                  </div>
                  
                  <div class="flex gap-3">
                    <p-button 
                      [label]="ls.t().accept" 
                      icon="pi pi-check" 
                      [severity]="event()?.userResponse === 'Accepted' ? 'success' : 'secondary'"
                      [outlined]="event()?.userResponse !== 'Accepted'"
                      (onClick)="rsvp('Accepted')"
                      [loading]="rsvpLoading()"
                    />
                    <p-button 
                      [label]="ls.t().decline" 
                      icon="pi pi-times" 
                      [severity]="event()?.userResponse === 'Declined' ? 'danger' : 'secondary'"
                      [outlined]="event()?.userResponse !== 'Declined'"
                      (onClick)="rsvp('Declined')"
                      [loading]="rsvpLoading()"
                    />
                  </div>
                </div>
              </p-card>
            </div>

            <div class="col-span-12 lg:col-span-4 flex flex-col gap-6">
              <p-card [header]="ls.t().attendees">
                <div class="flex flex-col gap-4">
                  <div class="flex justify-between items-center">
                    <span class="text-surface-600">{{ ls.t().accept }}</span>
                    <p-tag severity="success" [value]="event()?.acceptedUsers?.length?.toString()"></p-tag>
                  </div>
                  <div class="flex flex-col gap-2 max-h-48 overflow-y-auto pr-2 custom-scrollbar">
                    @for (user of event()?.acceptedUsers; track user.userId) {
                      <div class="flex items-center gap-2 p-2 bg-surface-50 dark:bg-surface-800 rounded-lg">
                        <div class="flex h-8 w-8 shrink-0 rounded-full bg-primary-500 text-white items-center justify-center text-xs font-bold">
                          {{ user.userName.substring(0, 1).toUpperCase() }}
                        </div>
                        <span class="text-sm font-medium">{{ user.userName }}</span>
                      </div>
                    } @empty {
                      <span class="text-xs text-surface-400 italic">Nog geen aanmeldingen</span>
                    }
                  </div>

                  <p-divider></p-divider>

                  <div class="flex justify-between items-center">
                    <span class="text-surface-600">{{ ls.t().declined }}</span>
                    <p-tag severity="danger" [value]="event()?.declinedUsers?.length?.toString()"></p-tag>
                  </div>
                  <div class="flex flex-col gap-2 max-h-48 overflow-y-auto pr-2 custom-scrollbar">
                    @for (user of event()?.declinedUsers; track user.userId) {
                      <div class="flex items-center gap-2 p-2 bg-surface-50 dark:bg-surface-800 rounded-lg opacity-60">
                        <div class="flex h-8 w-8 shrink-0 rounded-full bg-surface-300 dark:bg-surface-600 text-surface-600 items-center justify-center text-xs font-bold">
                          {{ user.userName.substring(0, 1).toUpperCase() }}
                        </div>
                        <span class="text-sm">{{ user.userName }}</span>
                      </div>
                    } @empty {
                      <span class="text-xs text-surface-400 italic">Nog geen afmeldingen</span>
                    }
                  </div>
                </div>
              </p-card>

              <p-card header="Informatie">
                <div class="flex flex-col gap-3 text-sm">
                  <div class="flex justify-between">
                    <span class="text-surface-500">Gemaakt door</span>
                    <span class="font-medium">{{ event()?.creatorName }}</span>
                  </div>
                  <div class="flex justify-between">
                    <span class="text-surface-500">Gemaakt op</span>
                    <span class="font-medium">{{ event()?.createdOn | date:'dd/MM/yyyy, HH:mm' }}</span>
                  </div>
                </div>
              </p-card>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .custom-scrollbar {
      scrollbar-width: thin;
      scrollbar-color: var(--p-surface-300) transparent;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class EventDetailComponent implements OnInit {
  private readonly eventService = inject(EventService);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  ls = inject(LanguageService);

  event = signal<EventDto | null>(null);
  loading = signal<boolean>(false);
  rsvpLoading = signal<boolean>(false);

  canEdit = computed(() => {
    const user = this.authService.currentUser();
    const currentEvent = this.event();
    if (!user || !currentEvent) return false;
    
    return user.roles.includes('Admin') || user.userName === currentEvent.creatorName;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadEvent(id);
    }
  }

  loadEvent(id: string): void {
    this.loading.set(true);
    this.eventService.getEvent(id).subscribe({
      next: (event) => {
        this.event.set(event);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Kan evenement niet laden' });
      }
    });
  }

  rsvp(status: 'Accepted' | 'Declined'): void {
    const currentEvent = this.event();
    if (!currentEvent) return;

    this.rsvpLoading.set(true);
    this.eventService.rsvp(currentEvent.id, status as EventResponseStatus).subscribe({
      next: () => {
        this.rsvpLoading.set(false);
        this.loadEvent(currentEvent.id);
        this.messageService.add({ 
            severity: 'success', 
            summary: 'Gedaan!', 
            detail: status === 'Accepted' ? 'Je bent aangemeld!' : 'Je bent afgemeld.' 
        });
      },
      error: () => {
        this.rsvpLoading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'RSVP mislukt' });
      }
    });
  }

  deleteEvent(): void {
    const currentEvent = this.event();
    if (!currentEvent) return;

    if (confirm(this.ls.t().confirm_delete_event)) {
      this.eventService.deleteEvent(currentEvent.id).subscribe({
        next: () => {
          this.router.navigate(['/events']);
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Verwijderen mislukt' });
        }
      });
    }
  }
}
