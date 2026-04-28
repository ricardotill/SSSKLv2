import { Component, ChangeDetectionStrategy, inject, signal, effect, ViewChild, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { DrawerModule } from 'primeng/drawer';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { UserProfileDrawerService } from '../../../core/services/user-profile-drawer.service';
import { ApplicationUserDto } from '../../../core/models/application-user.model';
import { environment } from '../../../../environments/environment';
import { ResolveApiUrlPipe } from '../../pipes/resolve-api-url.pipe';
import { AuthService } from '../../../core/auth/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { EditorModule } from 'primeng/editor';
import { FormsModule } from '@angular/forms';
import { ProcessedContentPipe } from '../../pipes/processed-content.pipe';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Router, RouterModule } from '@angular/router';
import { AchievementService } from '../../../features/achievements/services/achievement.service';
import { AchievementEntry } from '../../../core/models/achievement.model';
import { TooltipModule } from 'primeng/tooltip';
import { QuoteService } from '../../../features/quotes/services/quote.service';
import { QuoteDto } from '../../../core/models/quote.model';

@Component({
  selector: 'app-user-profile-drawer',
  standalone: true,
  imports: [
    CommonModule,
    DrawerModule,
    ButtonModule,
    ProgressSpinnerModule,
    ResolveApiUrlPipe,
    EditorModule,
    FormsModule,
    ProcessedContentPipe,
    ConfirmDialogModule,
    RouterModule,
    TooltipModule
  ],
  template: `
    <p-drawer 
      [visible]="drawerService.drawerVisible()"
      (visibleChange)="onVisibleChange($event)"
      position="bottom"
      [style]="{ height: 'auto', maxHeight: '90vh' }"
      styleClass="user-profile-drawer rounded-t-3xl shadow-2xl"
      [baseZIndex]="10000"
      [modal]="false"
      [blockScroll]="false"
      [appendTo]="'body'"
    >
      <ng-template pTemplate="header">
        <div class="flex items-center gap-3">
          <i class="pi pi-user text-2xl text-primary"></i>
          <h2 class="text-xl font-bold m-0">{{ user()?.fullName || ls.t().profile }}</h2>
        </div>
      </ng-template>

      @if (loading()) {
        <div class="flex justify-center items-center p-12">
          <p-progressSpinner></p-progressSpinner>
        </div>
      } @else if (user(); as u) {
        <div class="flex flex-col md:flex-row gap-6 p-2">
          
          <div class="flex flex-col items-center gap-4 min-w-[200px]">
            <div class="relative group mt-2">
              <div class="w-40 h-40 border-4 border-primary shadow-lg rounded-full overflow-hidden flex items-center justify-center bg-surface-100 dark:bg-surface-800">
                @if (u.profilePictureUrl; as url) {
                  <img [src]="url | resolveApiUrl" class="w-full h-full object-cover" alt="Profile picture" />
                } @else {
                  <span class="text-6xl font-bold uppercase text-surface-500 dark:text-surface-400">
                    {{ u.fullName.substring(0,1) || '?' }}
                  </span>
                }
              </div>
              
              @if (isCurrentUser() && u.profilePictureUrl) {
                <button 
                  (click)="onDeleteProfilePicture()"
                  class="absolute top-0 right-0 bg-red-500 text-white rounded-full p-2.5 shadow-md hover:bg-red-600 transition-colors z-10"
                  [title]="ls.t().delete"
                >
                  <i class="pi pi-trash text-xs"></i>
                </button>
              }
            </div>

            @if (isCurrentUser()) {
              <div class="flex flex-col items-center gap-2">
                <input type="file" #fileInput (change)="fileChangeEvent($event)" accept="image/*" class="hidden" />
                <p-button 
                  [label]="ls.t().change_picture" 
                  icon="pi pi-camera" 
                  size="small"
                  (onClick)="fileInput.click()"
                  [loading]="isUploadingPicture()"
                  outlined
                ></p-button>
              </div>
            }
            
            <div class="text-center w-full mt-2">
              <h3 class="text-2xl font-bold m-0">{{ u.fullName }}</h3>
              <p class="text-surface-500 my-1">&#64;{{ u.userName }}</p>
            </div>
            
            @if (isCurrentUser()) {
                <p-button 
                  [label]="ls.t().user_settings" 
                  icon="pi pi-cog" 
                  size="small"
                  severity="secondary"
                  (onClick)="goToSettings()"
                  class="w-full"
                  styleClass="w-full"
                ></p-button>
            }
          </div>

          <div class="flex-1 border-t md:border-t-0 md:border-l border-surface-200 dark:border-surface-700 pt-6 md:pt-0 md:pl-6 flex flex-col gap-6">
            <!-- Description Section -->
            <section>
              <div class="flex justify-between items-center mb-4">
                <h4 class="text-xl font-semibold m-0 flex items-center gap-2">
                  <i class="pi pi-info-circle text-primary"></i> 
                  {{ ls.t().description || 'Over mij' }}
                </h4>
                @if (isCurrentUser()) {
                  @if (!editMode()) {
                    <p-button icon="pi pi-pencil" [rounded]="true" [text]="true" (onClick)="startEdit()"></p-button>
                  } @else {
                    <div class="flex gap-2">
                      <p-button icon="pi pi-times" severity="secondary" [rounded]="true" [text]="true" (onClick)="cancelEdit()"></p-button>
                      <p-button icon="pi pi-check" [rounded]="true" [text]="true" (onClick)="saveDescription()" [loading]="saving()"></p-button>
                    </div>
                  }
                }
              </div>
              
              @if (editMode()) {
                <p-editor [(ngModel)]="editDescription" [style]="{ height: '180px' }">
                  <ng-template pTemplate="header">
                    <span class="ql-formats">
                        <button type="button" class="ql-bold" aria-label="Bold"></button>
                        <button type="button" class="ql-italic" aria-label="Italic"></button>
                        <button type="button" class="ql-underline" aria-label="Underline"></button>
                    </span>
                    <span class="ql-formats">
                        <button type="button" class="ql-list" value="ordered" aria-label="Ordered List"></button>
                        <button type="button" class="ql-list" value="bullet" aria-label="Bullet List"></button>
                    </span>
                  </ng-template>
                </p-editor>
              } @else {
                @if (u.description) {
                  <div class="prose dark:prose-invert max-w-none text-surface-700 dark:text-surface-300 bg-surface-50 dark:bg-surface-800/50 p-4 rounded-xl" [innerHTML]="u.description | processedContent"></div>
                } @else {
                   <div class="p-8 text-center bg-surface-50 dark:bg-surface-800/50 rounded-xl border border-dashed border-surface-200 dark:border-surface-700">
                      <i class="pi pi-id-card text-4xl text-surface-400 mb-3 opacity-50"></i>
                      <p class="text-surface-500 m-0 italic">{{ isCurrentUser() ? 'Voeg een beschrijving toe over jezelf.' : 'Deze gebruiker heeft nog geen beschrijving toegevoegd.' }}</p>
                   </div>
                }
              }
            </section>

            <!-- Achievements Section -->
            <section class="border-t border-surface-200 dark:border-surface-700 pt-6">
              <div class="flex justify-between items-center mb-4">
                <h4 
                  (click)="navigateToAchievements()" 
                  class="text-xl font-semibold m-0 flex items-center gap-2 cursor-pointer hover:text-primary transition-colors group"
                >
                  <i class="pi pi-trophy text-primary"></i> 
                  {{ ls.t().achievements }}
                  <i class="pi pi-chevron-right text-sm opacity-0 group-hover:opacity-100 transition-opacity"></i>
                </h4>
              </div>

              @if (achievements().length > 0) {
                <div class="flex flex-wrap gap-4">
                  @for (ach of achievements(); track ach.id) {
                    <div 
                      (click)="navigateToAchievementDetail(ach.achievementId)"
                      [pTooltip]="ach.achievementName"
                      tooltipPosition="top"
                      class="flex flex-col items-center gap-1 cursor-pointer hover:scale-105 transition-transform"
                    >
                      <div class="w-14 h-14 rounded-xl bg-surface-100 dark:bg-surface-800 flex items-center justify-center overflow-hidden border border-surface-200 dark:border-surface-700 shadow-sm">
                        @if (ach.imageUrl) {
                          <img [src]="ach.imageUrl | resolveApiUrl" class="w-full h-full object-contain p-1" [alt]="ach.achievementName" />
                        } @else {
                          <i class="pi pi-verified text-2xl text-primary"></i>
                        }
                      </div>
                      <span class="text-[10px] font-medium text-surface-500 dark:text-surface-400 max-w-[56px] truncate text-center">{{ ach.achievementName }}</span>
                    </div>
                  }
                </div>
              } @else {
                <div class="p-6 text-center bg-surface-50 dark:bg-surface-800/50 rounded-xl border border-dashed border-surface-200 dark:border-surface-700">
                  <p class="text-surface-500 m-0 italic text-sm">{{ ls.t().no_achievements }}</p>
                </div>
              }
            </section>

            <!-- Quotes Section -->
            @if (hasQuoteAccess()) {
              <section class="border-t border-surface-200 dark:border-surface-700 pt-6">
                <div class="flex justify-between items-center mb-4">
                  <h4 
                    (click)="navigateToQuotes()" 
                    class="text-xl font-semibold m-0 flex items-center gap-2 cursor-pointer hover:text-primary transition-colors group"
                  >
                    <i class="pi pi-comment text-primary"></i> 
                    {{ ls.t().quotes }}
                    <i class="pi pi-chevron-right text-sm opacity-0 group-hover:opacity-100 transition-opacity"></i>
                  </h4>
                </div>
  
                @if (quotes().length > 0) {
                  <div class="flex flex-col gap-3">
                    @for (quote of quotes(); track quote.id) {
                      <div 
                        (click)="navigateToQuoteDetail(quote.id)"
                        class="p-4 bg-surface-50 dark:bg-surface-800/50 rounded-xl border border-surface-200 dark:border-surface-700 cursor-pointer hover:border-primary transition-colors"
                      >
                        <p class="m-0 italic text-surface-900 dark:text-surface-0 line-clamp-2">"{{ quote.text }}"</p>
                        <div class="flex justify-between items-center mt-2">
                          <span class="text-xs text-surface-500">
                            {{ quote.dateSaid | date:'dd MMM yyyy' }}
                          </span>
                          <div class="flex items-center gap-3">
                            <div class="flex items-center gap-1 text-xs text-surface-500">
                              <i class="pi pi-heart-fill text-red-400"></i>
                              <span>{{ quote.voteCount || 0 }}</span>
                            </div>
                            <div class="flex items-center gap-1 text-xs text-surface-500">
                              <i class="pi pi-comments text-primary-400"></i>
                              <span>{{ quote.commentsCount || 0 }}</span>
                            </div>
                          </div>
                        </div>
                      </div>
                    }
                  </div>
                } @else {
                  <div class="p-6 text-center bg-surface-50 dark:bg-surface-800/50 rounded-xl border border-dashed border-surface-200 dark:border-surface-700">
                    <p class="text-surface-500 m-0 italic text-sm">Geen quotes gevonden.</p>
                  </div>
                }
              </section>
            }
          </div>
        </div>
      } @else {
        <div class="p-8 text-center text-surface-500">
          <i class="pi pi-exclamation-triangle text-4xl mb-3 text-red-400"></i>
          <p>Gebruiker niet gevonden.</p>
        </div>
      }
    </p-drawer>
  `,
  styles: [`
    .user-profile-drawer {
      height: auto !important;
    }
    :host ::ng-deep .user-profile-drawer .p-drawer-content {
      padding-top: 0;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserProfileDrawerComponent {
  drawerService = inject(UserProfileDrawerService);
  private http = inject(HttpClient);
  authService = inject(AuthService);
  ls = inject(LanguageService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private router = inject(Router);
  private achievementService = inject(AchievementService);
  private quoteService = inject(QuoteService);

  user = signal<ApplicationUserDto | null>(null);
  achievements = signal<AchievementEntry[]>([]);
  quotes = signal<QuoteDto[]>([]);
  hasQuoteAccess = signal(true);
  loading = signal(false);
  editMode = signal(false);
  saving = signal(false);
  editDescription = '';
  
  isUploadingPicture = signal(false);
  @ViewChild('fileInput') fileInput!: any;

  constructor() {
    effect(() => {
      const id = this.drawerService.selectedUserId();
      if (id) {
        this.loadUser(id);
      } else {
        this.user.set(null);
        this.achievements.set([]);
        this.quotes.set([]);
        this.hasQuoteAccess.set(true);
        this.editMode.set(false);
      }
    });
  }

  @HostListener('document:mousedown', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (!this.drawerService.drawerVisible()) return;

    const target = event.target as HTMLElement;
    
    // Check if click is inside the drawer (appended to body)
    const isInsideDrawer = !!target.closest('.user-profile-drawer');
    
    // Safety check: clicking the trigger (like leaderboard name or avatar) should be handled by the trigger itself.
    // If we're clicking outside the drawer, we close it.
    // Since profile drawer can be opened from many places, we mostly rely on checking if it's NOT in the drawer.
    // We also check for PrimeNG overlays (like confirmation dialogs) to avoid closing when a dialog is open.
    const isOverlay = !!target.closest('.p-dialog-mask') || !!target.closest('.p-confirm-dialog');

    if (!isInsideDrawer && !isOverlay) {
      // Small delay to allow potential trigger clicks to finish
      // but actually, mousedown happens first. 
      // If we clicked a trigger, it will set visible=true again anyway if using a toggle, 
      // but profile drawer is usually just "open".
      this.drawerService.close();
    }
  }

  isCurrentUser(): boolean {
    const current = this.authService.currentUser();
    const u = this.user();
    return !!(current && u && current.userName === u.userName);
  }

  onVisibleChange(visible: boolean) {
    if (!visible) {
      this.drawerService.close();
    }
  }

  loadUser(id: string) {
    this.loading.set(true);
    this.editMode.set(false);
    this.http.get<ApplicationUserDto>(`${environment.apiUrl}/api/v1/ApplicationUser/${id}/profile`).subscribe({
      next: (data) => {
        this.user.set(data);
        this.editDescription = data.description || '';
        this.loading.set(false);
      },
      error: () => {
        this.user.set(null);
        this.loading.set(false);
      }
    });

    // Fetch achievements
    this.achievementService.getAchievementEntries(id).subscribe({
      next: (data) => {
        // Sort by dateAdded descending and take top 4
        const sorted = [...data].sort((a, b) => new Date(b.dateAdded).getTime() - new Date(a.dateAdded).getTime());
        this.achievements.set(sorted.slice(0, 4));
      },
      error: () => this.achievements.set([])
    });

    // Fetch quotes
    this.quoteService.getQuotes(0, 3, id).subscribe({
      next: (data) => {
        this.quotes.set(data);
        this.hasQuoteAccess.set(true);
      },
      error: (err) => {
        this.quotes.set([]);
        if (err.status === 403) {
          this.hasQuoteAccess.set(false);
        }
      }
    });
  }

  startEdit() {
    if (!this.isCurrentUser()) return;
    this.editDescription = this.user()?.description || '';
    this.editMode.set(true);
  }

  cancelEdit() {
    this.editMode.set(false);
  }

  saveDescription() {
    if (!this.isCurrentUser()) return;
    this.saving.set(true);
    this.authService.updateMe({ description: this.editDescription }).subscribe({
      next: () => {
        this.saving.set(false);
        this.editMode.set(false);
        const current = this.user();
        if (current) {
          this.user.set({ ...current, description: this.editDescription });
        }
        this.messageService.add({ severity: 'success', summary: 'Opgeslagen', detail: 'Beschrijving bijgewerkt.' });
      },
      error: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Kon beschrijving niet bijwerken.' });
      }
    });
  }
  
  goToSettings() {
    this.drawerService.close();
    this.router.navigate(['/settings']);
  }

  navigateToAchievements() {
    const userId = this.user()?.id;
    this.drawerService.close();
    this.router.navigate(['/achievements'], { queryParams: { userId } });
  }

  navigateToAchievementDetail(achievementId: string) {
    this.drawerService.close();
    this.router.navigate(['/achievements', achievementId]);
  }

  navigateToQuotes() {
    const userId = this.user()?.id;
    this.drawerService.close();
    this.router.navigate(['/quotes'], { queryParams: { userId } });
  }

  navigateToQuoteDetail(quoteId: string) {
    this.drawerService.close();
    this.router.navigate(['/quotes', quoteId]);
  }

  // Profile Picture Methods
  fileChangeEvent(event: any): void {
    if (event.target.files && event.target.files.length > 0) {
      const file = event.target.files[0];
      this.uploadProfilePicture(file);
    }
  }

  private uploadProfilePicture(file: File) {
    this.isUploadingPicture.set(true);

    this.authService.uploadProfilePicture('', file).subscribe({
      next: () => {
        this.isUploadingPicture.set(false);
        if (this.fileInput) {
          this.fileInput.nativeElement.value = '';
        }
        
        // Refresh auth service current user, which will emit event, it doesn't refresh the drawer profile picture unless we reload user or update model directly. Let's just reload.
        const id = this.drawerService.selectedUserId();
        if (id) this.loadUser(id);
        
        this.messageService.add({
          severity: 'success',
          summary: this.ls.t().success || 'Success',
          detail: 'Profielfoto geupdatet.'
        });
      },
      error: (err) => {
        this.isUploadingPicture.set(false);
        this.messageService.add({
          severity: 'error',
          summary: this.ls.t().error || 'Error',
          detail: 'Kon profielfoto niet uploaden.'
        });
      }
    });
  }

  onDeleteProfilePicture() {
    this.confirmationService.confirm({
      message: 'Weet je zeker dat je je profielfoto wilt verwijderen?',
      header: 'Verwijder Profielfoto',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.authService.deleteProfilePicture().subscribe({
          next: () => {
            const id = this.drawerService.selectedUserId();
            if (id) this.loadUser(id);
            this.messageService.add({
              severity: 'success',
              summary: this.ls.t().success || 'Success',
              detail: 'Profielfoto verwijderd.'
            });
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: this.ls.t().error || 'Error',
              detail: 'Kon profielfoto niet verwijderen.'
            });
          }
        });
      }
    });
  }
}
