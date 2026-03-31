import { Component, ChangeDetectionStrategy, inject, signal, OnInit, computed, effect } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { DataViewModule } from 'primeng/dataview';
import { TagModule } from 'primeng/tag';
import { ImageModule } from 'primeng/image';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { AchievementService } from './services/achievement.service';
import { ApplicationUserService } from '../users/services/application-user.service';
import { AuthService } from '../../core/auth/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { AchievementListing } from '../../core/models/achievement.model';
import { ApplicationUserDto } from '../../core/models/application-user.model';

@Component({
  selector: 'app-achievements',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    SelectModule,
    CardModule,
    DataViewModule,
    TagModule,
    ImageModule,
    ProgressSpinnerModule,
    DatePipe
  ],
  template: `
    <div class="flex flex-col gap-4">
      <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t()['achievements'] }}</h1>
          <p class="text-surface-500 m-0">{{ ls.t()['achievements_desc'] }}</p>
        </div>
        
        <div class="w-full md:w-72">
          <p-select 
            [options]="users()" 
            [(ngModel)]="selectedUser" 
            optionLabel="fullName" 
            [filter]="true" 
            filterBy="fullName" 
            [placeholder]="ls.t()['select_user']"
            class="w-full"
            (onChange)="onUserChange($event.value)"
          >
            <ng-template pTemplate="selectedItem" let-selectedOption>
                <div class="flex items-center gap-2" *ngIf="selectedOption">
                    <div>{{ selectedOption.fullName }}</div>
                </div>
            </ng-template>
            <ng-template let-user pTemplate="item">
                <div class="flex items-center gap-2">
                    <div>{{ user.fullName }}</div>
                </div>
            </ng-template>
          </p-select>
        </div>
      </div>

      @if (loading()) {
        <div class="flex justify-center items-center p-12">
          <p-progressSpinner ariaLabel="loading"></p-progressSpinner>
        </div>
      } @else {
        <p-dataView [value]="sortedEntries()" layout="grid">
          <ng-template #grid let-items>
            <div class="grid grid-cols-12 gap-4">
              @for (entry of items; track entry.name) {
                <div
                  class="col-span-12 sm:col-span-6 md:col-span-4 xl:col-span-3 cursor-pointer"
                  [routerLink]="['/achievements', entry.id]"
                >
                  <p-card class="h-full achievement-card" [class.locked]="!entry.completed">
                    <ng-template pTemplate="header">
                      <div class="flex justify-center bg-surface-50/50 dark:bg-surface-800/50 rounded-t-lg items-center overflow-hidden min-h-[12rem]">
                        @if (entry.imageUrl) {
                          <p-image 
                            [src]="entry.imageUrl" 
                            [alt]="entry.name" 
                            imageClass="max-h-[150px] object-contain drop-shadow-md hover:scale-110 transition-transform duration-300"
                          ></p-image>
                        } @else {
                          <i class="pi pi-verified text-8xl" [class.text-primary-500]="entry.completed" [class.text-gray-400]="!entry.completed"></i>
                        }
                      </div>
                    </ng-template>
                    
                    <div class="flex flex-col gap-2 h-full">
                      <div class="flex justify-between items-start gap-2">
                        <span class="font-bold text-lg text-surface-900 dark:text-surface-0">{{ entry.name }}</span>
                        @if (!entry.completed) {
                          <p-tag [value]="ls.t()['locked']" severity="secondary"></p-tag>
                        }
                      </div>
                      
                      <p class="text-surface-600 dark:text-surface-400 text-sm line-clamp-3 mb-4">
                        {{ entry.description }}
                      </p>
                      
                      @if (entry.completed) {
                        <div class="mt-auto pt-4 border-t border-surface-200 dark:border-surface-700 flex flex-col gap-1">
                          <span class="text-xs text-surface-500 uppercase">{{ ls.t()['achieved_on'] }}</span>
                          <span class="text-sm font-medium">{{ entry.dateAdded | date:'mediumDate' }}</span>
                        </div>
                      } @else {
                         <div class="mt-auto pt-4 border-t border-surface-200 dark:border-surface-700 flex flex-col gap-1">
                          <span class="text-xs text-surface-500 uppercase opacity-0">.</span>
                          <span class="text-sm font-medium text-surface-400 italic">{{ ls.t()['not_achieved_yet'] }}</span>
                        </div>
                      }
                    </div>
                  </p-card>
                </div>
              }
            </div>
          </ng-template>
          
          <ng-template #emptymessage>
            <div class="flex flex-col items-center justify-center p-12 text-surface-500 bg-surface-50 dark:bg-surface-800 rounded-lg border border-dashed border-surface-300 dark:border-surface-600">
              <i class="pi pi-trophy text-6xl mb-4 opacity-20"></i>
              <p>{{ ls.t()['no_achievements'] }}</p>
            </div>
          </ng-template>
        </p-dataView>
      }
    </div>
  `,
  styles: [`
    :host ::ng-deep .p-dataview-content {
      background: transparent !important;
      border: none !important;
    }
    :host ::ng-deep .p-card {
      height: 100%;
      display: flex;
      flex-direction: column;
      transition: all 0.3s ease-in-out;
    }
    :host ::ng-deep .p-card-header {
       background: transparent !important;
    }
    :host ::ng-deep .p-card-body {
      flex: 1;
      display: flex;
      flex-direction: column;
    }
    :host ::ng-deep .p-card-content {
      flex: 1;
      padding: 0;
    }
    .achievement-card:hover {
      transform: translateY(-4px);
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
    }
    .locked {
      filter: grayscale(1) opacity(0.7);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class AchievementsComponent implements OnInit {
  private readonly achievementService = inject(AchievementService);
  private readonly userService = inject(ApplicationUserService);
  private readonly authService = inject(AuthService);
  ls = inject(LanguageService);

  users = signal<ApplicationUserDto[]>([]);
  entries = signal<AchievementListing[]>([]);
  loading = signal<boolean>(false);
  selectedUser: ApplicationUserDto | null = null;
  private hasInitializedSelection = false;

  constructor() {
    effect(() => {
      const currentUser = this.authService.currentUser();
      const usersList = this.users();

      if (currentUser && usersList.length > 0 && !this.hasInitializedSelection) {
        this.hasInitializedSelection = true;
        this.selectedUser = usersList.find(u => u.id === currentUser.id) || null;
        if (this.selectedUser) {
          this.loadAchievements(this.selectedUser.id);
        }
      }
    });
  }

  sortedEntries = computed(() => {
    const rawData = this.entries();
    if (!rawData) return [];

    return [...rawData].sort((a, b) => {
      // 1. Completed first
      if (a.completed && !b.completed) return -1;
      if (!a.completed && b.completed) return 1;

      // 2. If both completed, sort by date added descending
      if (a.completed && b.completed) {
        const dateA = a.dateAdded ? new Date(a.dateAdded).getTime() : 0;
        const dateB = b.dateAdded ? new Date(b.dateAdded).getTime() : 0;
        if (dateB !== dateA) return dateB - dateA;
      }

      // 3. Fallback: Name
      return (a.name || '').localeCompare(b.name || '');
    });
  });

  isCurrentUserSelected = computed(() => {
    const current = this.authService.currentUser();
    return current && this.selectedUser && current.id === this.selectedUser.id;
  });

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.userService.getUsers().subscribe({
      next: (data) => {
        this.users.set(data.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onUserChange(user: ApplicationUserDto): void {
    if (user) {
      this.loadAchievements(user.id);
    }
  }

  loadAchievements(userId: string): void {
    this.loading.set(true);
    this.achievementService.getAllForUser(userId).subscribe({
      next: (entries) => {
        this.entries.set(entries);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
