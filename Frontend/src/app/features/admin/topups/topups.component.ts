import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { finalize } from 'rxjs';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { TopUpService } from '../../../core/services/top-up.service';
import { ApplicationUserService } from '../../../core/services/application-user.service';
import { TopUpDto, TopUpCreateDto } from '../../../core/models/top-up.model';
import { ApplicationUserDto } from '../../../core/models/application-user.model';
import { LanguageService } from '../../../core/services/language.service';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-topups',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CurrencyPipe,
    TableModule,
    ButtonModule,
    DialogModule,
    InputNumberModule,
    AutoCompleteModule,
    ConfirmDialogModule,
    CardModule
  ],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().top_ups }}</h1>
      <div class="flex gap-2">
        <p-button icon="pi pi-plus" severity="info" [rounded]="true" (onClick)="openCreateDialog()" [ariaLabel]="ls.t().add_top_up"></p-button>
        <p-button icon="pi pi-refresh" [rounded]="true" (onClick)="loadTopUps()" [ariaLabel]="ls.t().refresh"></p-button>
      </div>
    </div>
    <p-confirmDialog></p-confirmDialog>
    <p-card>
      <p-table 
        stripedRows
        [value]="topUps()" 
        [loading]="loading()" 
        [paginator]="true" 
        [rows]="15" 
        [totalRecords]="totalRecords()" 
        [lazy]="true"
        (onLazyLoad)="onLazyLoad($event)"
        responsiveLayout="scroll">
        <ng-template pTemplate="header">
          <tr>
            <th>{{ ls.t().username }}</th>
            <th>{{ ls.t().amount }}</th>
            <th class="w-24 text-center">{{ ls.t().actions }}</th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-topUp>
          <tr>
            <td>{{ topUp.userName }}</td>
            <td class="font-medium text-primary">{{ topUp.saldo | currency:'EUR' }}</td>
            <td class="text-center">
              <p-button icon="pi pi-trash" [rounded]="true" [text]="true" severity="danger" (onClick)="confirmDelete(topUp)" [loading]="deletingTopUpId() === topUp.id" [ariaLabel]="ls.t().delete"></p-button>
            </td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="3" class="text-center p-4 text-surface-500">{{ ls.t().no_top_ups }}</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>

    <p-dialog [header]="ls.t().add_top_up" [(visible)]="dialogVisible" [modal]="true" [style]="{width: '500px', minHeight: '400px'}" [contentStyle]="{minHeight: '300px'}" [breakpoints]="{'768px': '90vw'}">
      <form [formGroup]="topUpForm" (ngSubmit)="saveTopUp()" class="flex flex-col gap-4 mt-2">
        <div class="flex flex-col gap-2">
          <label for="userName">{{ ls.t().user }}</label>
          <p-autocomplete 
            id="userName" 
            formControlName="userName" 
            [suggestions]="filteredUsers()" 
            (completeMethod)="filterUsers($event)"
            optionLabel="fullName"
            dataKey="id"
            [dropdown]="true"
            [forceSelection]="true"
            [placeholder]="ls.t().select_user"
            class="w-full"
            inputStyleClass="w-full">
            <ng-template pTemplate="item" let-user>
                <div class="flex flex-col">
                    <span class="font-medium">{{ user.fullName || user.userName }}</span>
                    @if (user.fullName) {
                        <span class="text-xs text-surface-500">{{ user.userName }}</span>
                    }
                </div>
            </ng-template>
          </p-autocomplete>
        </div>

        <div class="flex flex-col gap-2">
          <label for="saldo">{{ ls.t().amount }}</label>
          <p-inputNumber id="saldo" formControlName="saldo" mode="currency" currency="EUR" locale="nl-NL" class="w-full" [min]="1"></p-inputNumber>
        </div>
      </form>
      
      <ng-template pTemplate="footer">
        <p-button [label]="ls.t().cancel" icon="pi pi-times" [text]="true" severity="secondary" (onClick)="dialogVisible.set(false)"></p-button>
        <p-button [label]="ls.t().save" icon="pi pi-check" (onClick)="saveTopUp()" [loading]="saving()" [disabled]="topUpForm.invalid"></p-button>
      </ng-template>
    </p-dialog>
  `,
  styles: ``,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService]
})
export default class TopUpsComponent implements OnInit {
  private readonly topUpService = inject(TopUpService);
  private readonly userService = inject(ApplicationUserService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly authService = inject(AuthService);
  ls = inject(LanguageService);

  topUps = signal<TopUpDto[]>([]);
  users = signal<ApplicationUserDto[]>([]);
  filteredUsers = signal<ApplicationUserDto[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  dialogVisible = signal<boolean>(false);
  saving = signal<boolean>(false);
  deletingTopUpId = signal<string | null>(null);

  topUpForm = this.fb.nonNullable.group({
    userName: [null as ApplicationUserDto | null, Validators.required],
    saldo: [5, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.userService.getUsers().subscribe({
      next: (data) => this.users.set(data.items),
      error: () => console.error('Failed to load users for topup')
    });
  }

  filterUsers(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredUsers.set(
      this.users().filter(u =>
        u.userName.toLowerCase().includes(query) ||
        (u.fullName && u.fullName.toLowerCase().includes(query))
      )
    );
  }

  loadTopUps(skip: number = 0, take: number = 15): void {
    this.loading.set(true);
    this.topUpService.getTopUps(skip, take).subscribe({
      next: (data) => {
        this.topUps.set(data.items);
        this.totalRecords.set(data.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        this.loading.set(false);
      }
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const skip = event.first ?? 0;
    const take = event.rows ?? 15;
    this.loadTopUps(skip, take);
  }

  openCreateDialog(): void {
    this.topUpForm.reset({
      userName: null,
      saldo: 5
    });
    this.dialogVisible.set(true);
  }

  saveTopUp(): void {
    if (this.topUpForm.invalid) return;

    this.saving.set(true);
    const formValue = this.topUpForm.getRawValue();
    const user = formValue.userName as unknown as ApplicationUserDto;

    const createDto: TopUpCreateDto = {
      userName: user.userName,
      saldo: formValue.saldo
    };

    this.topUpService.createTopUp(createDto).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().top_up_added });
        this.authService.refreshCurrentUser();
        this.dialogVisible.set(false);
        this.saving.set(false);
        this.loadTopUps();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().error });
        this.saving.set(false);
      }
    });
  }

  confirmDelete(topUp: TopUpDto): void {
    this.confirmationService.confirm({
      message: this.ls.t().confirm_delete_top_up,
      header: this.ls.t().confirm_delete_title,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.deleteTopUp(topUp.id);
      }
    });
  }

  deleteTopUp(id: string): void {
    this.deletingTopUpId.set(id);
    this.topUpService.deleteTopUp(id)
      .pipe(finalize(() => this.deletingTopUpId.set(null)))
      .subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().top_up_deleted });
          this.authService.refreshCurrentUser();
          this.loadTopUps();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().delete_failed });
        }
      });
  }
}
