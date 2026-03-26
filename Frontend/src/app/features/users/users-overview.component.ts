import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { ApplicationUserService } from '../../core/services/application-user.service';
import { ApplicationUserDto } from '../../core/models/application-user.model';
import { LanguageService } from '../../core/services/language.service';

@Component({
  selector: 'app-users-overview',
  standalone: true,
  imports: [CurrencyPipe, TableModule, CardModule, ButtonModule],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().user_overview }}</h1>
      <p-button icon="pi pi-refresh" [rounded]="true" (onClick)="loadUsers()" [ariaLabel]="ls.t().refresh"></p-button>
    </div>
    
    <p-card>
      <p-table stripedRows [value]="users()" [loading]="loading()" responsiveLayout="scroll">
        <ng-template pTemplate="header">
          <tr>
            <th pSortableColumn="fullName">{{ ls.t().name }} <p-sortIcon field="fullName"></p-sortIcon></th>
            <th pSortableColumn="saldo">{{ ls.t().balance }} <p-sortIcon field="saldo"></p-sortIcon></th>
          </tr>
        </ng-template>
        <ng-template pTemplate="body" let-user>
          <tr>
            <td>{{ user.fullName }}</td>
            <td>{{ user.saldo | currency:'EUR' }}</td>
          </tr>
        </ng-template>
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="2" class="text-center p-4 text-surface-500">{{ ls.t().no_users }}</td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class UsersOverviewComponent implements OnInit {
  private readonly userService = inject(ApplicationUserService);
  ls = inject(LanguageService);

  users = signal<ApplicationUserDto[]>([]);
  loading = signal<boolean>(false);

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.userService.getObscuredUsers().subscribe({
      next: (data) => {
        // Sort by saldo ascending as requested
        const sortedData = [...data].sort((a, b) => a.saldo - b.saldo);
        this.users.set(sortedData);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }
}
