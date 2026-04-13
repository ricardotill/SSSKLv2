import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { EventService } from '../services/event.service';
import { LanguageService } from '../../../core/services/language.service';
import { AuthService } from '../../../core/auth/auth.service';
import { EventDto, EventResponseStatus } from '../../../core/models/event.model';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageService } from 'primeng/api';
import { Meta, Title } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
import { AvatarModule } from 'primeng/avatar';
import { ResolveApiUrlPipe } from '../../../shared/pipes/resolve-api-url.pipe';
import { UrlService } from '../../../core/services/url.service';
import { ProcessedContentPipe } from '../../../shared/pipes/processed-content.pipe';

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
    AvatarModule,
    ResolveApiUrlPipe,
    ProcessedContentPipe
  ],
  template: `
    <div class="max-w-4xl mx-auto">
      
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
              <p-button [label]="ls.t().share" icon="pi pi-share-alt" [outlined]="true" severity="secondary" (onClick)="share()" />
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
                    <img [src]="event()?.imageUrl | resolveApiUrl" alt="Event Banner" class="w-full h-64 object-cover">
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

                  @if (event()?.requiredRoles && event()!.requiredRoles!.length > 0) {
                    <div class="flex flex-col gap-2 mb-4">
                      <span class="text-xs text-surface-500 uppercase font-bold tracking-wider">Toegestane Rollen</span>
                      <div class="flex gap-2 flex-wrap">
                        @for (role of event()?.requiredRoles; track role) {
                          <p-tag severity="secondary" [value]="role" icon="pi pi-lock"></p-tag>
                        }
                      </div>
                    </div>
                  }

                  <div class="rich-text-content prose dark:prose-invert max-w-none" 
                       style="word-wrap: break-word; overflow-wrap: break-word; word-break: normal; white-space: normal;"
                       [innerHTML]="event()?.description | processedContent">
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
                    @if (authService.isAuthenticated()) {
                      @if (canRsvp()) {
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
                      } @else {
                        <div class="flex items-center gap-2 text-surface-500 bg-surface-100 dark:bg-surface-800 p-2 rounded-lg">
                          <i class="pi pi-lock"></i>
                          <span class="text-sm font-medium">Je hebt niet de juiste rol om je aan te melden.</span>
                        </div>
                      }
                    } @else {
                      <p-button [label]="ls.t().login" icon="pi pi-sign-in" [routerLink]="['/login']" [queryParams]="{ returnUrl: router.url }" />
                    }
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
                  <div class="flex flex-col gap-2 mb-2 px-2">
                    <div class="flex -space-x-2">
                      @for (user of event()?.acceptedUsers?.slice(0, 5); track user.userId) {
                        <p-avatar 
                          [image]="(user.profilePictureUrl | resolveApiUrl) || undefined" 
                          [label]="!user.profilePictureUrl ? user.userName.substring(0,1) : undefined"
                          shape="circle" 
                          size="normal"
                          styleClass="ring-2 ring-surface-0 dark:ring-surface-900 shadow-sm"
                        ></p-avatar>
                      }
                      @if ((event()?.acceptedUsers?.length ?? 0) > 5) {
                        <div class="flex h-8 w-8 rounded-full ring-2 ring-surface-0 dark:ring-surface-900 bg-surface-200 dark:bg-surface-700 items-center justify-center text-xs font-bold text-surface-600 dark:text-surface-300 shadow-sm">
                          +{{ (event()?.acceptedUsers?.length ?? 0) - 5 }}
                        </div>
                      }
                    </div>
                  </div>
                  <div class="flex flex-col gap-2 max-h-48 overflow-y-auto pr-2 custom-scrollbar">
                    @for (user of event()?.acceptedUsers; track user.userId) {
                      <div class="flex items-center gap-2 p-2 bg-surface-50 dark:bg-surface-800 rounded-lg">
                        <p-avatar 
                          [image]="(user.profilePictureUrl | resolveApiUrl) || undefined" 
                          [label]="!user.profilePictureUrl ? user.userName.substring(0,1) : undefined"
                          shape="circle" 
                          size="normal"
                        ></p-avatar>
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
                        <p-avatar 
                          [image]="(user.profilePictureUrl | resolveApiUrl) || undefined" 
                          [label]="!user.profilePictureUrl ? user.userName.substring(0,1) : undefined"
                          shape="circle" 
                          size="normal"
                        ></p-avatar>
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
                  <div class="flex justify-between items-center">
                    <span class="text-surface-500">Gemaakt door</span>
                    <div class="flex items-center gap-2">
                      <p-avatar 
                        [image]="(event()?.creatorProfilePictureUrl | resolveApiUrl) || undefined" 
                        [label]="!event()?.creatorProfilePictureUrl ? event()?.creatorName?.substring(0,1) : undefined"
                        shape="circle" 
                        size="normal"
                        styleClass="w-6 h-6 text-[10px]"
                      ></p-avatar>
                      <span class="font-medium">{{ event()?.creatorName }}</span>
                    </div>
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
    :host ::ng-deep .rich-text-content * {
      word-break: normal !important;
      overflow-wrap: break-word !important;
      white-space: normal !important;
    }
    :host ::ng-deep .rich-text-content ul, 
    :host ::ng-deep .rich-text-content ol {
      margin-bottom: 1rem;
      padding-left: 1.5rem;
    }
    :host ::ng-deep .rich-text-content ul {
      list-style-type: disc;
    }
    :host ::ng-deep .rich-text-content ol {
      list-style-type: decimal;
    }
    :host ::ng-deep .rich-text-content li {
      margin-bottom: 0.5rem;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class EventDetailComponent implements OnInit {
  private readonly eventService = inject(EventService);
  public readonly authService = inject(AuthService);
  private readonly activatedRoute = inject(ActivatedRoute);
  public readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly meta = inject(Meta);
  private readonly titleService = inject(Title);
  private readonly document = inject(DOCUMENT);
  private readonly urlService = inject(UrlService);
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

  canRsvp = computed(() => {
    const user = this.authService.currentUser();
    const evt = this.event();
    if (!user || !evt) return false;

    if (user.roles.includes('Admin')) return true;
    if (!evt.requiredRoles || evt.requiredRoles.length === 0) return true;

    return evt.requiredRoles.some(role => user.roles.includes(role));
  });

  ngOnInit(): void {
    const id = this.activatedRoute.snapshot.paramMap.get('id');
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
        this.updateMetaTags(event);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Kan evenement niet laden' });
      }
    });
  }

  updateMetaTags(event: EventDto): void {
    const title = `${event.title} - SSSKL`;
    const description = this.stripHtml(event.description).substring(0, 160);
    const url = this.document.location.href;
    const image = this.urlService.resolveApiUrl(event.imageUrl) || '';

    this.titleService.setTitle(title);

    // OG Tags for WhatsApp/Facebook
    this.meta.updateTag({ property: 'og:title', content: title });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    if (image) {
      this.meta.updateTag({ property: 'og:image', content: `${image}/social-preview.jpg` });
    }
  }

  share(): void {
    const currentEvent = this.event();
    if (!currentEvent) return;

    if (navigator.share) {
      navigator.share({
        title: currentEvent.title,
        text: "Meld je nu aan voor " + currentEvent.title + "!",
        url: window.location.href
      }).catch(err => console.error('Share failed', err));
    } else {
      // Fallback to clipboard
      navigator.clipboard.writeText(window.location.href).then(() => {
        this.messageService.add({ severity: 'success', summary: 'Gekopieerd', detail: 'Link gekopieerd naar klembord' });
      });
    }
  }

  private stripHtml(html: string | undefined): string {
    if (!html) return '';
    return html.replace(/<[^>]*>?/gm, '');
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
