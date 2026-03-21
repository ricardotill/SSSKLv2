import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';
import { TagModule } from "primeng/tag";
import { CheckboxModule } from 'primeng/checkbox';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    CardModule,
    MessageModule,
    TagModule,
    CheckboxModule,
    RouterModule
  ],
  template: `
    <div class="login-wrapper">
      <p-card class="login-card">
        <ng-template #header>
          <div class="flex items-center justify-between p-5 pb-0">
            <div class="flex items-center">
              <h2 class="text-xl font-bold m-0">SSSKL</h2> <p-tag class="ml-2" value="v2" />
            </div>
            <p-button icon="pi pi-arrow-left" label="Back" [text]="true" routerLink="/" severity="secondary" size="small"></p-button>
          </div>
        </ng-template>

        <div class="mb-5">
          <h3 class="text-xl font-medium mt-0 mb-3 text-surface-900 dark:text-surface-0">Login</h3>  
          <p class="text-surface-500 m-0">Don't have an account? <a routerLink="/register" class="text-primary hover:underline cursor-pointer">Register</a></p>
        </div>

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
          
          <div class="field">
            <label for="userName" class="block">Username</label>
            <input 
              id="userName" 
              type="text" 
              pInputText 
              formControlName="userName" 
              class="w-full"
            />
            @if (loginForm.controls['userName'].invalid && loginForm.controls['userName'].touched) {
               <small class="p-error block mt-1">Username is required.</small>
            }
          </div>

          <div class="field mt-4">
            <label for="password" class="block">Password</label>
            <p-password 
              id="password" 
              formControlName="password" 
              [feedback]="false" 
              [toggleMask]="true"
              styleClass="w-full"
              inputStyleClass="w-full"
            ></p-password>
            @if (loginForm.controls['password'].invalid && loginForm.controls['password'].touched) {
               <small class="p-error block mt-1">Password is required.</small>
            }
          </div>

          <div class="field-checkbox mt-4 flex items-center">
            <p-checkbox formControlName="rememberMe" [binary]="true" inputId="rememberMe"></p-checkbox>
            <label for="rememberMe" class="ml-2 font-medium text-surface-900 dark:text-surface-0 cursor-pointer">Remember me</label>
          </div>

          @if (errorMessage()) {
             <p-message severity="error" [text]="errorMessage()" styleClass="w-full mt-4"></p-message>
          }

          <div class="mt-4">
            <p-button 
              label="Login" 
              type="submit" 
              [disabled]="loginForm.invalid || isLoading()" 
              [loading]="isLoading()"
              styleClass="w-full">
            </p-button>
          </div>
        </form>
      </p-card>
    </div>
  `,
  styles: [`
    .login-wrapper {
      display: flex;
      justify-content: center;
      align-items: center;
      height: 100vh;
      background-color: var(--surface-ground);
    }
    .login-card {
      width: 100%;
      max-width: 400px;
    }
    .field {
      margin-bottom: 1.5rem;
    }
    .block {
      display: block;
    }
    .w-full {
      width: 100%;
    }
    .mt-4 {
      margin-top: 1rem;
    }
    .mt-1 {
      margin-top: 0.25rem;
    }
    ::ng-deep .p-checkbox .p-checkbox-box {
      border-radius: 4px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  loginForm = this.fb.group({
    userName: ['', Validators.required],
    password: ['', Validators.required],
    rememberMe: [false]
  });

  isLoading = signal(false);
  errorMessage = signal('');

  onSubmit(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const credentials = {
      userName: this.loginForm.value.userName,
      password: this.loginForm.value.password
    };

    const rememberMe = this.loginForm.value.rememberMe ?? false;

    this.authService.login(credentials, rememberMe).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/']); // Redirect to dashboard or returnUrl
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set('Invalid username or password.');
        console.error('Login error', err);
      }
    });
  }
}
