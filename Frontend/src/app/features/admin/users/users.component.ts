import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { finalize } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ApplicationUserService } from '../../../core/services/application-user.service';
import { ApplicationUserDto, ApplicationUserUpdateDto } from '../../../core/models/application-user.model';
import { LanguageService } from '../../../core/services/language.service';
import { CardModule } from 'primeng/card';
import { MultiSelectModule } from 'primeng/multiselect';


@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    CurrencyPipe,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    CheckboxModule,
    ConfirmDialogModule,
    CardModule,
    MultiSelectModule
  ],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().users }}</h1>
      <p-button icon="pi pi-refresh" [rounded]="true" (onClick)="loadUsers()" [ariaLabel]="ls.t().refresh"></p-button>
    </div>
    <p-confirmDialog></p-confirmDialog>
    <p-card>
      
      <p-table stripedRows [value]="users()" [loading]="loading()" [paginator]="true" [rows]="10" [totalRecords]="totalRecords()" responsiveLayout="scroll">
        <ng-template pTemplate="header">
          <tr>
            <th>{{ ls.t().username }}</th>
            <th>{{ ls.t().full_name }}</th>
            <th>{{ ls.t().balance }}</th>
            <th>{{ ls.t().last_ordered }}</th>
            <th class="w-32">{{ ls.t().actions }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-user>
          <tr>
            <td>{{ user.userName }}</td>
            <td>{{ user.fullName }}</td>
            <td>{{ user.saldo | currency:'EUR' }}</td>
            <td>{{ user.lastOrdered ? (user.lastOrdered | date:'dd-MM-yyyy HH:mm') : ls.t().never }}</td>
            <td>
              <div class="flex gap-2">
                <p-button icon="pi pi-pencil" [rounded]="true" [text]="true" severity="info" (onClick)="openEditDialog(user)" [ariaLabel]="ls.t().edit"></p-button>
                <p-button icon="pi pi-trash" [rounded]="true" [text]="true" severity="danger" (onClick)="confirmDelete(user)" [loading]="deletingUserId() === user.id" [ariaLabel]="ls.t().delete"></p-button>
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="5" class="text-center p-4 text-surface-500">{{ ls.t().no_users }}</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>

    <p-dialog [header]="ls.t().edit_user" [(visible)]="editDialogVisible" [modal]="true" [style]="{width: '500px'}" [breakpoints]="{'768px': '90vw'}">
      <form [formGroup]="editForm" (ngSubmit)="saveEdit()" class="flex flex-col gap-4 mt-2">
        <div class="flex flex-col gap-2">
          <label for="userName">{{ ls.t().username }}</label>
          <input pInputText id="userName" formControlName="userName" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="name">{{ ls.t().first_name }}</label>
          <input pInputText id="name" formControlName="name" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="surname">{{ ls.t().last_name }}</label>
          <input pInputText id="surname" formControlName="surname" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="email">{{ ls.t().email }}</label>
          <input pInputText id="email" type="email" formControlName="email" class="w-full" />
        </div>

        <div class="flex items-center gap-2">
          <p-checkbox formControlName="emailConfirmed" [binary]="true" inputId="emailConfirmed"></p-checkbox>
          <label for="emailConfirmed">{{ ls.t().email_confirmed }}</label>
        </div>

        <div class="flex flex-col gap-2">
          <label for="phoneNumber">{{ ls.t().phone_number }}</label>
          <input pInputText id="phoneNumber" formControlName="phoneNumber" class="w-full" />
        </div>

        <div class="flex items-center gap-2">
          <p-checkbox formControlName="phoneNumberConfirmed" [binary]="true" inputId="phoneNumberConfirmed"></p-checkbox>
          <label for="phoneNumberConfirmed">{{ ls.t().phone_number_confirmed }}</label>
        </div>

        <div class="flex flex-col gap-2">
          <label for="password">{{ ls.t().new_password_help }}</label>
          <input pInputText id="password" type="password" formControlName="password" class="w-full" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="roles">{{ ls.t().roles }}</label>
          <p-multiSelect id="roles" formControlName="roles" [options]="availableRoles" optionLabel="label" optionValue="value" [placeholder]="ls.t().select_roles" class="w-full" display="chip"></p-multiSelect>
        </div>
      </form>
      
      <ng-template pTemplate="footer">
        <p-button [label]="ls.t().cancel" icon="pi pi-times" [text]="true" severity="secondary" (onClick)="editDialogVisible.set(false)"></p-button>
        <p-button [label]="ls.t().save" icon="pi pi-check" (onClick)="saveEdit()" [loading]="saving()" [disabled]="editForm.invalid"></p-button>
      </ng-template>
    </p-dialog>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService]
})
export default class UsersComponent implements OnInit {
  private readonly userService = inject(ApplicationUserService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  ls = inject(LanguageService);

  users = signal<ApplicationUserDto[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);

  editDialogVisible = signal<boolean>(false);
  editingUserId = signal<string | null>(null);
  saving = signal<boolean>(false);
  deletingUserId = signal<string | null>(null);

  editForm = this.fb.nonNullable.group({
    userName: [{ value: '', disabled: true }, Validators.required],
    email: [''],
    emailConfirmed: [false],
    phoneNumber: [''],
    phoneNumberConfirmed: [false],
    name: [''],
    surname: [''],
    password: [''],
    roles: [[] as string[]]
  });

  availableRoles = [
    { label: 'Guest', value: 'Guest' },
    { label: 'User', value: 'User' },
    { label: 'Kiosk', value: 'Kiosk' },
    { label: 'Admin', value: 'Admin' }
  ];

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.userService.getUsers().subscribe({
      next: (data) => {
        this.users.set(data.items);
        this.totalRecords.set(data.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        this.loading.set(false);
      }
    });
  }

  openEditDialog(user: ApplicationUserDto): void {
    this.editingUserId.set(user.id);
    this.saving.set(false);
    this.editForm.reset();

    // Set dialog visible immediately while loading details to give faster feedback
    this.editDialogVisible.set(true);

    this.userService.getUser(user.id).subscribe({
      next: (details) => {
        this.editForm.patchValue({
          userName: details.userName,
          email: details.email ?? '',
          emailConfirmed: details.emailConfirmed,
          phoneNumber: details.phoneNumber ?? '',
          phoneNumberConfirmed: details.phoneNumberConfirmed,
          name: details.name ?? '',
          surname: details.surname ?? '',
          password: '',
          roles: details.roles ?? []
        });
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        this.editDialogVisible.set(false);
      }
    });
  }

  saveEdit(): void {
    if (this.editForm.invalid) return;

    const id = this.editingUserId();
    if (!id) return;

    this.saving.set(true);
    const formValue = this.editForm.getRawValue();

    // Construct DTO taking care to omit empty strings for optional fields if necessary, 
    // or passing them. Usually empty strings are okay if the backend accepts it.
    const updateDto: ApplicationUserUpdateDto = {
      userName: formValue.userName,
      email: formValue.email || undefined,
      emailConfirmed: formValue.emailConfirmed,
      phoneNumber: formValue.phoneNumber || undefined,
      phoneNumberConfirmed: formValue.phoneNumberConfirmed,
      name: formValue.name || undefined,
      surname: formValue.surname || undefined,
      // Only send password if it has been typed
      ...(formValue.password ? { password: formValue.password } : {}),
      roles: formValue.roles
    };

    this.userService.updateUser(id, updateDto).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().user_updated });
        this.editDialogVisible.set(false);
        this.saving.set(false);
        this.loadUsers(); // refresh the list
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().update_failed });
        this.saving.set(false);
      }
    });
  }

  confirmDelete(user: ApplicationUserDto): void {
    this.confirmationService.confirm({
      message: this.ls.translate('confirm_delete_user', { user: user.userName }),
      header: this.ls.t().confirm_delete_title,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteUser(user.id);
      }
    });
  }

  deleteUser(id: string): void {
    this.deletingUserId.set(id);
    this.userService.deleteUser(id)
      .pipe(finalize(() => this.deletingUserId.set(null)))
      .subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().user_deleted });
          this.loadUsers();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().delete_failed });
        }
      });
  }
}

