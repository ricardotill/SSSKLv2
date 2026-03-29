import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { RoleService } from '../../../core/services/role.service';
import { Role } from '../../../core/models/role.model';
import { LanguageService } from '../../../core/services/language.service';
import { DialogModule } from 'primeng/dialog';
import { CardModule } from 'primeng/card';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    ReactiveFormsModule,
    TableModule, 
    ButtonModule, 
    InputTextModule, 
    ConfirmDialogModule,
    DialogModule,
    CardModule
  ],
  providers: [ConfirmationService],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().roles_title }}</h1>
      <div class="flex gap-2">
        <p-button icon="pi pi-refresh" [rounded]="true" [text]="true" (onClick)="loadRoles()" [ariaLabel]="ls.t().refresh"></p-button>
        <p-button icon="pi pi-plus" [label]="ls.t().roles_add" (onClick)="showAddDialog()"></p-button>
      </div>
    </div>

    <p-confirmDialog></p-confirmDialog>

    <p-card>
      <p-table 
        [value]="roles()" 
        [loading]="loading()" 
        [rowHover]="true" 
        stripedRows
        [paginator]="true" 
        [rows]="10"
        responsiveLayout="scroll">
        <ng-template pTemplate="header">
          <tr>
            <th>{{ ls.t().roles_name }}</th>
            <th class="w-32">{{ ls.t().roles_actions }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-role>
          <tr>
            <td>
              <span class="font-medium">{{ role.name }}</span>
              @if (isSystemRole(role.name)) {
                <span class="ml-2 px-2 py-0.5 text-xs bg-surface-100 dark:bg-surface-800 text-surface-600 dark:text-surface-400 rounded-full border border-surface-200 dark:border-surface-700">
                  System
                </span>
              }
            </td>
            <td>
              <div class="flex gap-2">
                <p-button 
                  icon="pi pi-pencil" 
                  [rounded]="true" 
                  [text]="true" 
                  severity="info" 
                  (onClick)="showEditDialog(role)" 
                  [disabled]="isSystemRole(role.name)"
                  [ariaLabel]="ls.t().edit">
                </p-button>
                <p-button 
                  *ngIf="!isSystemRole(role.name)" 
                  icon="pi pi-trash" 
                  [rounded]="true" 
                  [text]="true" 
                  severity="danger" 
                  (onClick)="confirmDelete(role)" 
                  [loading]="deletingRoleId() === role.id"
                  [ariaLabel]="ls.t().delete">
                </p-button>
              </div>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="2" class="text-center p-4 text-surface-500">{{ ls.t().roles_no_roles }}</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>

    <p-dialog 
      [(visible)]="displayDialog" 
      [header]="editingRole() ? ls.t().roles_edit_role || 'Rol Bewerken' : ls.t().roles_add_role" 
      [modal]="true" 
      [style]="{width: '450px'}"
      [breakpoints]="{'768px': '90vw'}">
      
      <form [formGroup]="roleForm" (ngSubmit)="saveRole()" class="flex flex-col gap-4 mt-2">
        <div class="flex flex-col gap-2">
          <label for="roleId" class="font-medium text-surface-700 dark:text-surface-300">ID</label>
          <input pInputText id="roleId" formControlName="id" class="w-full bg-surface-50 dark:bg-surface-900" [placeholder]="editingRole() ? '' : '(Nieuwe Rol)'" />
        </div>

        <div class="flex flex-col gap-2">
          <label for="roleName" class="font-medium text-surface-700 dark:text-surface-300">{{ ls.t().roles_name }}</label>
          <input pInputText id="roleName" formControlName="name" class="w-full" autofocus />
          @if (roleForm.get('name')?.invalid && roleForm.get('name')?.touched) {
            <small class="text-red-500">{{ ls.t().required }}</small>
          }
        </div>
      </form>

      <ng-template pTemplate="footer">
        <p-button 
          [label]="ls.t().cancel" 
          icon="pi pi-times" 
          [text]="true" 
          severity="secondary" 
          (onClick)="displayDialog.set(false)">
        </p-button>
        <p-button 
          [label]="ls.t().save" 
          icon="pi pi-check" 
          (onClick)="saveRole()" 
          [loading]="saving()" 
          [disabled]="roleForm.invalid">
        </p-button>
      </ng-template>
    </p-dialog>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RolesComponent implements OnInit {
  private roleService = inject(RoleService);
  private confirmationService = inject(ConfirmationService);
  private messageService = inject(MessageService);
  private fb = inject(FormBuilder);
  public ls = inject(LanguageService);

  roles = signal<Role[]>([]);
  loading = signal<boolean>(false);
  displayDialog = signal<boolean>(false);
  saving = signal<boolean>(false);
  editingRole = signal<Role | null>(null);
  deletingRoleId = signal<string | null>(null);

  roleForm = this.fb.group({
    id: [{ value: '', disabled: true }],
    name: ['', [Validators.required, Validators.minLength(2)]]
  });

  protected systemRoles = ['Admin', 'User', 'Guest', 'Kiosk'];

  ngOnInit() {
    this.loadRoles();
  }

  loadRoles() {
    this.loading.set(true);
    this.roleService.getAdminRoles().subscribe({
      next: (data) => {
        this.roles.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({ 
            severity: 'error', 
            summary: this.ls.t().error, 
            detail: this.ls.t().load_failed 
        });
        this.loading.set(false);
      }
    });
  }

  showAddDialog() {
    this.editingRole.set(null);
    this.roleForm.reset();
    this.displayDialog.set(true);
  }

  showEditDialog(role: Role) {
    this.editingRole.set(role);
    this.roleForm.patchValue({
      id: role.id,
      name: role.name
    });
    this.displayDialog.set(true);
  }

  saveRole() {
    if (this.roleForm.invalid) return;

    this.saving.set(true);
    const formValue = this.roleForm.getRawValue();
    const roleToSave = { name: formValue.name! };

    const operation = this.editingRole() 
        ? this.roleService.updateRole(this.editingRole()!.id, roleToSave)
        : this.roleService.createRole(roleToSave);

    operation.pipe(
        finalize(() => this.saving.set(false))
    ).subscribe({
      next: () => {
        this.loadRoles();
        this.displayDialog.set(false);
        this.messageService.add({ 
            severity: 'success', 
            summary: this.ls.t().success, 
            detail: this.editingRole() ? this.ls.t().role_updated || 'Rol bijgewerkt' : this.ls.t().role_created 
        });
      },
      error: (err) => {
        this.messageService.add({ 
            severity: 'error', 
            summary: this.ls.t().error, 
            detail: err.error?.title || (this.editingRole() ? 'Bijwerken rol mislukt' : this.ls.t().role_creation_failed)
        });
      }
    });
  }

  confirmDelete(role: Role) {
    this.confirmationService.confirm({
      message: this.ls.translate('confirm_delete_role', { role: role.name }),
      header: this.ls.t().confirm_delete_title || 'Bevestig Verwijdering',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteRole(role);
      }
    });
  }

  deleteRole(role: Role) {
    this.deletingRoleId.set(role.id);
    this.roleService.deleteRole(role.id).pipe(
        finalize(() => this.deletingRoleId.set(null))
    ).subscribe({
      next: () => {
        this.roles.set(this.roles().filter(r => r.id !== role.id));
        this.messageService.add({ 
            severity: 'success', 
            summary: this.ls.t().success, 
            detail: this.ls.t().role_deleted 
        });
      },
      error: (err) => {
        this.messageService.add({ 
            severity: 'error', 
            summary: this.ls.t().error, 
            detail: err.error || this.ls.t().role_deletion_failed 
        });
      }
    });
  }

  isSystemRole(name: string): boolean {
    return this.systemRoles.some(sr => sr.toLowerCase() === name.toLowerCase());
  }
}
