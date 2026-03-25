import { Component, ChangeDetectionStrategy, inject, signal, OnInit, ViewChild, ElementRef } from '@angular/core';
import { finalize } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { PickListModule } from 'primeng/picklist';
import { CheckboxModule } from 'primeng/checkbox';
import { FileUploadModule } from 'primeng/fileupload';
import { NgIf, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AchievementService } from '../../../core/services/achievement.service';
import { ApplicationUserService } from '../../../core/services/application-user.service';
import { Achievement, AchievementUpdateDto, ActionOption, ComparisonOperatorOption, PaginationObject } from '../../../core/models/achievement.model';
import { ApplicationUserDto } from '../../../core/models/application-user.model';
import { LanguageService } from '../../../core/services/language.service';

interface PickListAchievement {
  id: string;
  name: string;
  imageUri?: string;
  entryId?: string;
}

@Component({
  selector: 'app-admin-achievements',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    ToastModule,
    ConfirmDialogModule,
    CardModule,
    SelectModule,
    PickListModule,
    CheckboxModule,
    FileUploadModule,
    NgIf,
    NgClass,
    FormsModule
  ],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">Achievements</h1>
      <div class="flex gap-2">
        <p-button icon="pi pi-users" severity="help" [rounded]="true" (onClick)="openUserDialog()" ariaLabel="Gebruikers Beheren"></p-button>
        <p-button icon="pi pi-plus" severity="info" [rounded]="true" (onClick)="openCreateDialog()" ariaLabel="Achievement Toevoegen"></p-button>
        <p-button icon="pi pi-refresh" [rounded]="true" (onClick)="loadAchievements()" ariaLabel="Vernieuwen"></p-button>
      </div>
    </div>
    <p-toast></p-toast>
    <p-confirmDialog></p-confirmDialog>
    
    <p-card>
      <p-table stripedRows [value]="achievements()" [loading]="loading()" [paginator]="true" [rows]="10" [totalRecords]="totalRecords()" responsiveLayout="scroll">
        <ng-template pTemplate="header">
          <tr>
            <th class="w-16">Icoon</th>
            <th>Naam</th>
            <th>Beschrijving</th>
            <th>Automatisch</th>
            <th>Voorwaarde</th>
            <th class="w-40">Acties</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-ach>
          <tr>
            <td>
              <img *ngIf="ach.image?.uri" [src]="ach.image?.uri" class="w-10 h-10 object-contain shadow-sm rounded-md" />
            </td>
            <td>{{ ach.name }}</td>
            <td>{{ ach.description }}</td>
            <td>
              <i class="pi" [ngClass]="{'text-green-500 pi-check-circle': ach.autoAchieve, 'text-red-500 pi-times-circle': !ach.autoAchieve}"></i>
            </td>
            <td>
              <span *ngIf="ach.autoAchieve" class="text-sm">
                {{ ach.action }} {{ ach.comparisonOperator }} {{ ach.comparisonValue }}
              </span>
              <span *ngIf="!ach.autoAchieve" class="text-sm text-surface-500">Handmatig</span>
            </td>
            <td>
              <div class="flex gap-2">
                <p-button icon="pi pi-users" [rounded]="true" [text]="true" severity="help" (onClick)="confirmAwardAll(ach)" ariaLabel="Aan Iedereen Toekennen"></p-button>
                <p-button icon="pi pi-pencil" [rounded]="true" [text]="true" severity="info" (onClick)="openEditDialog(ach)" ariaLabel="Bewerken"></p-button>
                <p-button icon="pi pi-trash" [rounded]="true" [text]="true" severity="danger" (onClick)="confirmDelete(ach)" [loading]="deletingId() === ach.id" ariaLabel="Verwijderen"></p-button>
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="6" class="text-center p-4 text-surface-500">Geen achievements gevonden</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>

    <p-dialog [header]="isEdit() ? 'Achievement Bewerken' : 'Achievement Toevoegen'" [(visible)]="dialogVisible" [modal]="true" [style]="{width: '500px'}" [breakpoints]="{'768px': '90vw'}">
      <form [formGroup]="achForm" (ngSubmit)="saveAchievement()" class="flex flex-col gap-4 mt-2">
        <div class="flex flex-col gap-2">
          <label for="name">Naam</label>
          <input pInputText id="name" formControlName="name" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="description">Beschrijving</label>
          <input pInputText id="description" formControlName="description" class="w-full" />
        </div>
        
        <div class="flex items-center gap-2">
          <p-checkbox inputId="autoAchieve" formControlName="autoAchieve" [binary]="true"></p-checkbox>
          <label for="autoAchieve">Automatisch Behalen</label>
        </div>

        @if (achForm.get('autoAchieve')?.value) {
          <div class="flex flex-col gap-2">
            <label for="action">Actie Optie</label>
            <p-select id="action" [options]="actionOptions" formControlName="action" optionLabel="label" optionValue="value" class="w-full" appendTo="body"></p-select>
          </div>

          <div class="flex flex-col gap-2">
            <label for="comparisonOperator">Vergelijkingsoperator</label>
            <p-select id="comparisonOperator" [options]="operatorOptions" formControlName="comparisonOperator" optionLabel="label" optionValue="value" class="w-full" appendTo="body"></p-select>
          </div>

          <div class="flex flex-col gap-2">
            <label for="comparisonValue">Vergelijkingswaarde</label>
            <p-inputNumber id="comparisonValue" formControlName="comparisonValue" class="w-full" [useGrouping]="false"></p-inputNumber>
          </div>
        }

        <div class="flex flex-col gap-2" *ngIf="!isEdit() || isEdit()">
          <label for="image">Afbeelding (.png, .jpg)</label>
          @if (isEdit() && currentImageUri()) {
            <div class="mb-2">
              <span class="text-sm text-surface-500 block mb-1">Huidige afbeelding:</span>
              <img [src]="currentImageUri()" class="w-16 h-16 object-contain rounded-md border border-surface-200 dark:border-surface-700 bg-surface-100 dark:bg-surface-800" />
            </div>
          }
          <input type="file" id="image" (change)="onFileSelected($event)" accept="image/png, image/jpeg" class="w-full p-2 border border-surface-200 dark:border-surface-700 rounded-md" />
        </div>
      </form>
      
      <ng-template pTemplate="footer">
        <p-button label="Annuleren" icon="pi pi-times" [text]="true" severity="secondary" (onClick)="dialogVisible.set(false)"></p-button>
        <p-button label="Opslaan" icon="pi pi-check" (onClick)="saveAchievement()" [loading]="saving()" [disabled]="achForm.invalid"></p-button>
      </ng-template>
    </p-dialog>
    
    <p-dialog header="Gebruiker Achievements Beheren" [(visible)]="userDialogVisible" [modal]="true" [style]="{width: '80vw'}" [maximizable]="true">
      <div class="flex flex-col gap-4 mt-2">
        <div class="flex flex-col gap-2">
          <label>Selecteer Gebruiker</label>
          <p-select [options]="users()" [(ngModel)]="selectedUser" optionLabel="fullName" [filter]="true" filterBy="fullName" [showClear]="true" placeholder="Selecteer een Gebruiker" (onChange)="onUserSelect($any($event).value)" appendTo="body" class="w-full md:w-1/2"></p-select>
        </div>
        
        @if (selectedUser()) {
          <p-pickList [source]="availableAchievements()" [target]="userAchievements()" sourceHeader="Beschikbaar" targetHeader="Achievements van Gebruiker" [dragdrop]="true" [responsive]="true" [sourceStyle]="{ height: '30rem' }" [targetStyle]="{ height: '30rem' }" filterBy="name" sourceFilterPlaceholder="Zoeken op naam" targetFilterPlaceholder="Zoeken op naam" [showSourceControls]="false" [showTargetControls]="false" (onMoveToTarget)="onMoveToTarget($event)" (onMoveToSource)="onMoveToSource($event)">
            <ng-template let-ach pTemplate="item">
              <div class="flex flex-wrap p-2 items-center gap-3">
                <img *ngIf="ach.imageUri" [src]="ach.imageUri" [alt]="ach.name" class="w-10 h-10 object-contain shadow-sm shrink-0 rounded-md bg-surface-100 dark:bg-surface-800" />
                <div class="flex-1 flex flex-col gap-1">
                    <span class="font-bold border-surface-200 dark:border-surface-700">{{ ach.name }}</span>
                </div>
              </div>
            </ng-template>
          </p-pickList>
        }
      </div>
    </p-dialog>
  `,
  providers: [MessageService, ConfirmationService]
})
export default class AchievementsComponent implements OnInit {
  private readonly achievementService = inject(AchievementService);
  private readonly userService = inject(ApplicationUserService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  ls = inject(LanguageService);

  achievements = signal<Achievement[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);

  dialogVisible = signal<boolean>(false);
  isEdit = signal<boolean>(false);
  editingId = signal<string | null>(null);
  currentImageUri = signal<string | null>(null);
  saving = signal<boolean>(false);
  deletingId = signal<string | null>(null);

  // Enums for dropdowns
  actionOptions = Object.values(ActionOption).map(v => ({ label: v, value: v }));
  operatorOptions = Object.values(ComparisonOperatorOption).map(v => ({ label: v, value: v }));

  selectedFile: File | null = null;

  achForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    autoAchieve: [false],
    action: [ActionOption.None],
    comparisonOperator: [ComparisonOperatorOption.None],
    comparisonValue: [0]
  });

  // User Management Signals
  userDialogVisible = signal<boolean>(false);
  users = signal<ApplicationUserDto[]>([]);
  selectedUser = signal<ApplicationUserDto | null>(null);

  allAchievementsForPick = signal<PickListAchievement[]>([]);
  availableAchievements = signal<PickListAchievement[]>([]);
  userAchievements = signal<PickListAchievement[]>([]);

  ngOnInit(): void {
    this.loadAchievements();
  }

  loadAchievements(): void {
    this.loading.set(true);
    // Passing 0 and 1000 to get a large list simply for our table and picklist for now.
    // In a real huge dataset, we should correctly wire up the table's (onLazyLoad) 
    this.achievementService.getAchievements(0, 1000).subscribe({
      next: (data) => {
        this.achievements.set(data.items);
        this.totalRecords.set(data.totalCount);
        this.loading.set(false);

        // Populate the base list for PickList
        this.allAchievementsForPick.set(data.items.map(a => ({
          id: a.id,
          name: a.name,
          imageUri: a.image?.uri
        })));

        // If a user is currently selected in the dialog, refresh their lists
        if (this.selectedUser()) {
          this.refreshUserPickList(this.selectedUser()!);
        }
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Laden van achievements is mislukt' });
        this.loading.set(false);
      }
    });
  }

  openCreateDialog(): void {
    this.isEdit.set(false);
    this.editingId.set(null);
    this.currentImageUri.set(null);
    this.selectedFile = null;
    this.achForm.reset({
      name: '',
      description: '',
      autoAchieve: false,
      action: ActionOption.None,
      comparisonOperator: ComparisonOperatorOption.None,
      comparisonValue: 0
    });
    this.dialogVisible.set(true);
  }

  openEditDialog(ach: Achievement): void {
    this.isEdit.set(true);
    this.editingId.set(ach.id);
    this.currentImageUri.set(ach.image?.uri || null);
    this.selectedFile = null;
    this.achForm.patchValue({
      name: ach.name,
      description: ach.description,
      autoAchieve: ach.autoAchieve,
      action: ach.action,
      comparisonOperator: ach.comparisonOperator,
      comparisonValue: ach.comparisonValue
    });
    this.dialogVisible.set(true);
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
    }
  }

  saveAchievement(): void {
    if (this.achForm.invalid) return;

    this.saving.set(true);
    const formValue = this.achForm.getRawValue();

    if (this.isEdit() && this.editingId()) {
      const updateDto: AchievementUpdateDto = {
        id: this.editingId()!,
        ...formValue
        // image cannot be updated easily via JSON PUT. 
      };
      this.achievementService.updateAchievement(updateDto).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Succes', detail: 'Achievement bewerkt' });
          this.dialogVisible.set(false);
          this.saving.set(false);
          this.loadAchievements();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Bewerken is mislukt' });
          this.saving.set(false);
        }
      });
    } else {
      const formData = new FormData();
      formData.append('Name', formValue.name);
      formData.append('Description', formValue.description);
      formData.append('AutoAchieve', String(formValue.autoAchieve));
      formData.append('Action', formValue.action);
      formData.append('ComparisonOperator', formValue.comparisonOperator);
      formData.append('ComparisonValue', String(formValue.comparisonValue));

      if (this.selectedFile) {
        formData.append('image', this.selectedFile);
      }

      this.achievementService.createAchievement(formData).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Succes', detail: 'Achievement toegevoegd' });
          this.dialogVisible.set(false);
          this.saving.set(false);
          this.loadAchievements();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Toevoegen is mislukt' });
          this.saving.set(false);
        }
      });
    }
  }

  confirmDelete(ach: Achievement): void {
    this.confirmationService.confirm({
      message: `Weet je zeker dat je "${ach.name}" wilt verwijderen?`,
      header: 'Verwijderen Bevestigen',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteAchievement(ach.id);
      }
    });
  }

  deleteAchievement(id: string): void {
    this.deletingId.set(id);
    this.achievementService.deleteAchievement(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Succes', detail: 'Achievement verwijderd' });
          this.loadAchievements();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Verwijderen is mislukt' });
        }
      });
  }

  confirmAwardAll(ach: Achievement): void {
    this.confirmationService.confirm({
      message: `Weet je zeker dat je "${ach.name}" aan ALLE gebruikers wilt toekennen?`,
      header: 'Toekennen aan Iedereen Bevestigen',
      icon: 'pi pi-users',
      acceptButtonStyleClass: 'p-button-help',
      accept: () => {
        this.achievementService.awardAchievementToAllUsers(ach.id).subscribe({
          next: (count) => {
            this.messageService.add({ severity: 'success', summary: 'Succes', detail: `Toegekend aan ${count} gebruikers` });
          },
          error: () => {
            this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Toekennen is mislukt' });
          }
        });
      }
    });
  }

  // --- USER ACHIEVEMENT MANAGEMENT ---

  openUserDialog(): void {
    this.userDialogVisible.set(true);
    if (this.users().length === 0) {
      this.userService.getObscuredUsers().subscribe(u => this.users.set(u));
    }
  }

  onUserSelect(user: ApplicationUserDto | null): void {
    this.selectedUser.set(user);
    if (!user) {
      this.availableAchievements.set([]);
      this.userAchievements.set([]);
      return;
    }
    this.refreshUserPickList(user);
  }

  private refreshUserPickList(user: ApplicationUserDto): void {
    this.achievementService.getAchievementEntries(user.id).subscribe({
      next: (entries) => {
        // Map entries to PickList formats
        const targetList: PickListAchievement[] = entries.map(e => ({
          id: e.achievementId,
          name: e.achievementName,
          imageUri: e.imageUrl,
          entryId: e.id
        }));

        // Filter out achievements the user already has from the source list
        const sourceList = this.allAchievementsForPick().filter(
          a => !targetList.some(t => t.id === a.id)
        );

        this.userAchievements.set(targetList);
        this.availableAchievements.set(sourceList);
      }
    });
  }

  onMoveToTarget(event: any): void {
    const user = this.selectedUser();
    if (!user) return;

    // Items moved to the right (awarded)
    const movedItems = event.items as PickListAchievement[];
    for (let ach of movedItems) {
      this.achievementService.awardAchievementToUser(user.id, ach.id).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Toegekend', detail: `${ach.name} is toegekend aan ${user.fullName}` });
          // Ideally fetch the entryId here to set on the item if they move it back immediately,
          // but for now re-fetching the list implicitly will solve it.
          // Since it's moved entirely by DOM, if they move it back immediately without an entryId, it might fail.
          // To be safe, we refresh the picklist.
          this.refreshUserPickList(user);
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Fout', detail: `Toekennen van ${ach.name} is mislukt` });
          this.refreshUserPickList(user);
        }
      });
    }
  }

  onMoveToSource(event: any): void {
    const user = this.selectedUser();
    if (!user) return;

    // Items moved to the left (removed)
    const movedItems = event.items as PickListAchievement[];
    const entryIdsToDelete = movedItems.filter(i => i.entryId).map(i => i.entryId!);

    if (entryIdsToDelete.length > 0) {
      this.achievementService.deleteAchievementEntries(entryIdsToDelete).subscribe({
        next: () => {
          this.messageService.add({ severity: 'info', summary: 'Verwijderd', detail: `${entryIdsToDelete.length} achievements verwijderd van ${user.fullName}` });
          this.refreshUserPickList(user);
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Verwijderen van achievements is mislukt' });
          this.refreshUserPickList(user);
        }
      });
    }
  }
}
