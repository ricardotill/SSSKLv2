import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { finalize } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { AnnouncementService } from '../../../core/services/announcement.service';
import { Announcement, AnnouncementCreateDto, AnnouncementUpdateDto } from '../../../core/models/announcement.model';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-announcements',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    CheckboxModule,
    InputNumberModule,
    DatePickerModule,
    ConfirmDialogModule,
    CardModule
  ],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().announcements }}</h1>
      <div class="flex gap-2">
        <p-button icon="pi pi-plus" severity="info" [rounded]="true" (onClick)="openCreateDialog()" [ariaLabel]="ls.t().add_announcement"></p-button>
        <p-button icon="pi pi-refresh" [rounded]="true" (onClick)="loadAnnouncements()" [ariaLabel]="ls.t().refresh"></p-button>
      </div>
    </div>

    <p-confirmDialog></p-confirmDialog>

    <p-card>
      <p-table stripedRows [value]="announcements()" [loading]="loading()" [paginator]="true" [rows]="10" responsiveLayout="scroll">
        <ng-template pTemplate="header">
          <tr>
            <th>{{ ls.t().message }}</th>
            <th class="w-24 text-center">{{ ls.t().announcement_order }}</th>
            <th class="w-24 text-center">{{ ls.t().is_scheduled }}</th>
            <th>{{ ls.t().planned_from }}</th>
            <th>{{ ls.t().planned_till }}</th>
            <th class="w-32">{{ ls.t().actions }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-announcement>
          <tr>
            <td>
              <div class="font-bold">{{ announcement.message }}</div>
              <div class="text-xs text-surface-600 dark:text-surface-400">{{ announcement.description }}</div>
            </td>
            <td class="text-center">{{ announcement.order }}</td>
            <td class="text-center">
              <i class="pi" [class.pi-check]="announcement.isScheduled" [class.text-green-500]="announcement.isScheduled" [class.pi-times]="!announcement.isScheduled" [class.text-red-500]="!announcement.isScheduled"></i>
            </td>
            <td>{{ announcement.plannedFrom ? (announcement.plannedFrom | date:'dd-MM-yyyy HH:mm') : '-' }}</td>
            <td>{{ announcement.plannedTill ? (announcement.plannedTill | date:'dd-MM-yyyy HH:mm') : '-' }}</td>
            <td>
              <div class="flex gap-2">
                <p-button icon="pi pi-pencil" [rounded]="true" [text]="true" severity="info" (onClick)="openEditDialog(announcement)" [ariaLabel]="ls.t().edit"></p-button>
                <p-button icon="pi pi-trash" [rounded]="true" [text]="true" severity="danger" (onClick)="confirmDelete(announcement)" [loading]="deletingId() === announcement.id" [ariaLabel]="ls.t().delete"></p-button>
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="6" class="text-center p-4 text-surface-500">{{ ls.t().no_announcements }}</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>

    <p-dialog [header]="editingId() ? ls.t().edit_announcement : ls.t().add_announcement" [(visible)]="dialogVisible" [modal]="true" [style]="{width: '500px'}" [breakpoints]="{'768px': '90vw'}">
      <form [formGroup]="announcementForm" (ngSubmit)="save()" class="flex flex-col gap-4 mt-2">
        <div class="flex flex-col gap-2">
          <label for="message">{{ ls.t().message }}</label>
          <input pInputText id="message" formControlName="message" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="description">{{ ls.t().product_description }}</label>
          <textarea pTextarea id="description" formControlName="description" class="w-full" rows="3"></textarea>
        </div>

        <div class="flex flex-col gap-2">
          <label for="order">{{ ls.t().announcement_order }}</label>
          <p-inputNumber id="order" formControlName="order" class="w-full"></p-inputNumber>
        </div>

        <div class="flex items-center gap-2">
          <p-checkbox formControlName="isScheduled" [binary]="true" inputId="isScheduled"></p-checkbox>
          <label for="isScheduled">{{ ls.t().is_scheduled }}</label>
        </div>

        @if (announcementForm.get('isScheduled')?.value) {
          <div class="flex flex-col gap-2">
            <label for="plannedFrom">{{ ls.t().planned_from }}</label>
            <p-datepicker id="plannedFrom" formControlName="plannedFrom" [showTime]="true" [showSeconds]="false" class="w-full" appendTo="body"></p-datepicker>
          </div>

          <div class="flex flex-col gap-2">
            <label for="plannedTill">{{ ls.t().planned_till }}</label>
            <p-datepicker id="plannedTill" formControlName="plannedTill" [showTime]="true" [showSeconds]="false" class="w-full" appendTo="body"></p-datepicker>
          </div>
        }
      </form>
      
      <ng-template pTemplate="footer">
        <p-button [label]="ls.t().cancel" icon="pi pi-times" [text]="true" severity="secondary" (onClick)="dialogVisible.set(false)"></p-button>
        <p-button [label]="ls.t().save" icon="pi pi-check" (onClick)="save()" [loading]="saving()" [disabled]="announcementForm.invalid"></p-button>
      </ng-template>
    </p-dialog>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService]
})
export default class AnnouncementsComponent implements OnInit {
  private readonly announcementService = inject(AnnouncementService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  ls = inject(LanguageService);

  announcements = signal<Announcement[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  dialogVisible = signal<boolean>(false);
  saving = signal<boolean>(false);
  deletingId = signal<string | null>(null);
  editingId = signal<string | null>(null);

  announcementForm = this.fb.nonNullable.group({
    message: ['', Validators.required],
    description: [''],
    order: [0, Validators.required],
    isScheduled: [false],
    plannedFrom: [null as Date | null],
    plannedTill: [null as Date | null]
  });

  ngOnInit(): void {
    this.loadAnnouncements();
  }

  loadAnnouncements(): void {
    this.loading.set(true);
    this.announcementService.getAnnouncements(0, 100).subscribe({
      next: (data) => {
        this.announcements.set(data.items);
        this.totalRecords.set(data.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        this.loading.set(false);
      }
    });
  }

  openCreateDialog(): void {
    this.editingId.set(null);
    this.announcementForm.reset({
      message: '',
      description: '',
      order: 0,
      isScheduled: false,
      plannedFrom: null,
      plannedTill: null
    });
    this.dialogVisible.set(true);
  }

  openEditDialog(announcement: Announcement): void {
    this.editingId.set(announcement.id);
    this.announcementForm.patchValue({
      message: announcement.message,
      description: announcement.description ?? '',
      order: announcement.order,
      isScheduled: announcement.isScheduled,
      plannedFrom: announcement.plannedFrom ? new Date(announcement.plannedFrom) : null,
      plannedTill: announcement.plannedTill ? new Date(announcement.plannedTill) : null
    });
    this.dialogVisible.set(true);
  }

  save(): void {
    if (this.announcementForm.invalid) return;

    this.saving.set(true);
    const formValue = this.announcementForm.getRawValue();

    const payload = {
      message: formValue.message,
      description: formValue.description || undefined,
      order: formValue.order,
      isScheduled: formValue.isScheduled,
      plannedFrom: formValue.isScheduled && formValue.plannedFrom ? formValue.plannedFrom.toISOString() : undefined,
      plannedTill: formValue.isScheduled && formValue.plannedTill ? formValue.plannedTill.toISOString() : undefined
    };

    if (this.editingId()) {
      this.announcementService.updateAnnouncement(this.editingId()!, payload as AnnouncementUpdateDto).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().announcement_updated });
          this.dialogVisible.set(false);
          this.saving.set(false);
          this.loadAnnouncements();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().update_failed });
          this.saving.set(false);
        }
      });
    } else {
      this.announcementService.createAnnouncement(payload as AnnouncementCreateDto).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().announcement_added });
          this.dialogVisible.set(false);
          this.saving.set(false);
          this.loadAnnouncements();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().error });
          this.saving.set(false);
        }
      });
    }
  }

  confirmDelete(announcement: Announcement): void {
    this.confirmationService.confirm({
      message: this.ls.t().confirm_delete_announcement,
      header: this.ls.t().confirm_delete_title,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteAnnouncement(announcement.id);
      }
    });
  }

  deleteAnnouncement(id: string): void {
    this.deletingId.set(id);
    this.announcementService.deleteAnnouncement(id)
      .pipe(finalize(() => this.deletingId.set(null)))
      .subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().announcement_deleted });
          this.loadAnnouncements();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().delete_failed });
        }
      });
  }
}
