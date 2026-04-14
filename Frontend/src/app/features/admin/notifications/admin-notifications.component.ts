import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelect } from 'primeng/multiselect';
import { CardModule } from 'primeng/card';
import { MessageService } from 'primeng/api';
import { ApplicationUserService } from '../../users/services/application-user.service';
import { NotificationService } from '../../../core/services/notification.service';
import { LanguageService } from '../../../core/services/language.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-admin-notifications',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    CheckboxModule,
    MultiSelect,
    CardModule
  ],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().admin_notifications }}</h1>
    </div>

    <p-card>
      <form [formGroup]="notificationForm" (ngSubmit)="sendNotification()" class="flex flex-col gap-6">
        <div class="flex flex-col gap-2">
          <label for="title" class="font-medium">{{ ls.t().notification_title }}</label>
          <input pInputText id="title" formControlName="title" [placeholder]="ls.t().notification_title" class="w-full" />
          @if (notificationForm.get('title')?.touched && notificationForm.get('title')?.invalid) {
            <small class="text-red-500">{{ ls.t().required }}</small>
          }
        </div>

        <div class="flex flex-col gap-2">
          <label for="message" class="font-medium">{{ ls.t().notification_message }}</label>
          <textarea pTextarea id="message" formControlName="message" [rows]="5" [autoResize]="true" [placeholder]="ls.t().notification_message" class="w-full"></textarea>
          @if (notificationForm.get('message')?.touched && notificationForm.get('message')?.invalid) {
            <small class="text-red-500">{{ ls.t().required }}</small>
          }
        </div>

        <div class="flex flex-col gap-2">
          <label for="linkUri" class="font-medium">{{ ls.t().notification_link }}</label>
          <input pInputText id="linkUri" formControlName="linkUri" [placeholder]="'/events/...'" class="w-full" />
          @if (notificationForm.get('linkUri')?.touched && notificationForm.get('linkUri')?.invalid) {
            <small class="text-red-500">Moet beginnen met '/' (bijv. /events)</small>
          }
          <small class="text-surface-500">{{ ls.t().notification_link_help }}</small>
        </div>

        <div class="flex items-center gap-2 py-2">
          <p-checkbox formControlName="fanOut" [binary]="true" inputId="fanOut"></p-checkbox>
          <label for="fanOut" class="cursor-pointer font-medium">{{ ls.t().fan_out }}</label>
        </div>

        @if (!notificationForm.get('fanOut')?.value) {
          <div class="flex flex-col gap-2 animate-fade-in">
            <label for="userIds" class="font-medium">{{ ls.t().select_users }}</label>
            <p-multiselect 
              id="userIds" 
              formControlName="userIds" 
              [options]="userOptions()" 
              [placeholder]="ls.t().select_users" 
              class="w-full" 
              display="chip"
              [filter]="true"
              filterBy="label"
              scrollHeight="250px"
              appendTo="body"
            ></p-multiselect>
            @if (notificationForm.get('userIds')?.touched && notificationForm.get('userIds')?.invalid) {
              <small class="text-red-500">{{ ls.t().required }}</small>
            }
          </div>
        }

        <div class="flex justify-end mt-4">
          <p-button 
            type="submit" 
            [label]="ls.t().send_notification" 
            icon="pi pi-send" 
            [loading]="sending()" 
            [disabled]="notificationForm.invalid"
            severity="primary"
          ></p-button>
        </div>
      </form>
    </p-card>
  `,
  styles: `
    .animate-fade-in {
      animation: fadeIn 0.3s ease-in-out;
    }
    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(-10px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export default class AdminNotificationsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(ApplicationUserService);
  private readonly notificationService = inject(NotificationService);
  private readonly messageService = inject(MessageService);
  ls = inject(LanguageService);

  sending = signal<boolean>(false);
  userOptions = signal<{ label: string, value: string }[]>([]);

  notificationForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    message: ['', Validators.required],
    linkUri: ['', [Validators.pattern('^/.*$')]],
    fanOut: [true],
    userIds: [[] as string[]]
  });

  ngOnInit(): void {
    this.notificationForm.get('fanOut')?.valueChanges.subscribe(fanOut => {
      const userIdsControl = this.notificationForm.get('userIds');
      if (fanOut) {
        userIdsControl?.clearValidators();
      } else {
        userIdsControl?.setValidators([Validators.required, Validators.minLength(1)]);
      }
      userIdsControl?.updateValueAndValidity();
    });

    this.loadUsers();
  }

  loadUsers(): void {
    this.userService.getAdminUsers(0, 5000).subscribe({
      next: (data) => {
        const options = data.items.map(u => ({
          label: `${u.fullName || u.userName} (${u.userName})`,
          value: u.id
        }));
        this.userOptions.set(options);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
      }
    });
  }

  sendNotification(): void {
    if (this.notificationForm.invalid) return;

    this.sending.set(true);
    const formValue = this.notificationForm.getRawValue();

    const dto = {
      title: formValue.title,
      message: formValue.message,
      linkUri: formValue.linkUri || undefined,
      fanOut: formValue.fanOut,
      userIds: formValue.fanOut ? undefined : formValue.userIds
    };

    this.notificationService.sendCustomNotification(dto)
      .pipe(finalize(() => this.sending.set(false)))
      .subscribe({
        next: () => {
          this.messageService.add({ 
            severity: 'success', 
            summary: this.ls.t().success, 
            detail: this.ls.t().notification_sent_success 
          });
          this.notificationForm.reset({
            title: '',
            message: '',
            linkUri: '',
            fanOut: true,
            userIds: []
          });
        },
        error: () => {
          this.messageService.add({ 
            severity: 'error', 
            summary: this.ls.t().error, 
            detail: this.ls.t().error 
          });
        }
      });
  }
}
