import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';
import { BrandingComponent } from '../../shared/components/branding/branding.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    CardModule,
    MessageModule,
    RouterModule,
    BrandingComponent
  ],
  template: `
    <div class="register-wrapper">
      <p-card class="register-card">
        <ng-template #header>
          <div class="flex items-center justify-between p-5 pb-0">
            <div class="flex items-center">
              <app-branding />
            </div>
            <p-button icon="pi pi-arrow-left" label="Back" [text]="true" routerLink="/" severity="secondary" size="small"></p-button>
          </div>
        </ng-template>
        
        <div class="mb-5">
            <h3 class="text-xl font-medium mt-0 mb-3 text-surface-900 dark:text-surface-0">Create an Account</h3>
            <p class="text-surface-500 m-0">Already have an account? <a routerLink="/login" class="text-primary hover:underline cursor-pointer">Login</a></p>
        </div>

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
          
          <div class="field">
            <label for="userName" class="block">Username</label>
            <input 
              id="userName" 
              type="text" 
              pInputText 
              formControlName="userName" 
              class="w-full"
            />
            @if (registerForm.controls['userName'].invalid && registerForm.controls['userName'].touched) {
               <small class="p-error block mt-1">Username is required.</small>
            }
          </div>
          
          <div class="field mt-4">
            <label for="email" class="block">Email</label>
            <input 
              id="email" 
              type="email" 
              pInputText 
              formControlName="email" 
              class="w-full"
            />
            @if (registerForm.controls['email'].invalid && registerForm.controls['email'].touched) {
               <small class="p-error block mt-1">A valid email is required.</small>
            }
          </div>
          
          <div class="flex gap-4 mt-4">
              <div class="field w-full mb-0">
                <label for="name" class="block">First Name</label>
                <input 
                  id="name" 
                  type="text" 
                  pInputText 
                  formControlName="name" 
                  class="w-full"
                />
                @if (registerForm.controls['name'].invalid && registerForm.controls['name'].touched) {
                   <small class="p-error block mt-1">First name is required.</small>
                }
              </div>
              
              <div class="field w-full mb-0">
                <label for="surname" class="block">Last Name</label>
                <input 
                  id="surname" 
                  type="text" 
                  pInputText 
                  formControlName="surname" 
                  class="w-full"
                />
                @if (registerForm.controls['surname'].invalid && registerForm.controls['surname'].touched) {
                   <small class="p-error block mt-1">Last name is required.</small>
                }
              </div>
          </div>

          <div class="field mt-4">
            <label for="password" class="block">Password</label>
            <p-password 
              id="password" 
              formControlName="password" 
              [feedback]="true" 
              [toggleMask]="true"
              styleClass="w-full"
              inputStyleClass="w-full"
            ></p-password>
            @if (registerForm.controls['password'].invalid && registerForm.controls['password'].touched) {
               <small class="p-error block mt-1">Password is required.</small>
            }
          </div>

          @if (errorMessage()) {
             <p-message severity="error" [text]="errorMessage()" styleClass="w-full mt-4"></p-message>
          }
          
          @if (successMessage()) {
             <p-message severity="success" [text]="successMessage()" styleClass="w-full mt-4"></p-message>
          }

          <div class="mt-4">
            <p-button 
              label="Register" 
              type="submit" 
              [disabled]="registerForm.invalid || isLoading()" 
              [loading]="isLoading()"
              styleClass="w-full">
            </p-button>
          </div>
        </form>
      </p-card>
    </div>
  `,
  styles: [`
    .register-wrapper {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background-color: var(--surface-ground);
      padding: 2rem 0;
    }
    .register-card {
      width: 100%;
      max-width: 450px;
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
    .mb-0 {
        margin-bottom: 0px !important;
    }
    .gap-4 {
        gap: 1rem;
    }
    .flex {
        display: flex;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  registerForm = this.fb.nonNullable.group({
    userName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    name: ['', Validators.required],
    surname: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  isLoading = signal(false);
  errorMessage = signal('');
  successMessage = signal('');

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    const userData = this.registerForm.getRawValue();

    this.authService.register(userData).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.successMessage.set('Registration successful! Redirecting to login...');
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: (err) => {
        this.isLoading.set(false);
        if (err.error?.errors) {
            const errors = err.error.errors;
            const firstErrorKey = Object.keys(errors)[0];
            this.errorMessage.set(errors[firstErrorKey][0]);
        } else if (err.error?.title) {
            this.errorMessage.set(err.error.title);
        } else {
            this.errorMessage.set('Registration failed. Please try again.');
        }
        console.error('Register error', err);
      }
    });
  }
}
