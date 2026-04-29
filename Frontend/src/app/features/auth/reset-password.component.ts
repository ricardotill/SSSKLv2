import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { LanguageService } from '../../core/services/language.service';
import { BrandingComponent } from '../../shared/components/branding/branding.component';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, InputTextModule, PasswordModule, ButtonModule, CardModule, MessageModule, RouterModule, BrandingComponent],
  template: `
    <div class="auth-wrapper">
      <p-card class="auth-card">
        <ng-template #header>
          <div class="flex items-center justify-between p-5 pb-0">
            <app-branding />
            <p-button icon="pi pi-arrow-left" [label]="lang.t().back" [text]="true" routerLink="/login" severity="secondary" size="small"></p-button>
          </div>
        </ng-template>

        <div class="mb-5">
          <h3 class="text-xl font-medium mt-0 mb-3 text-surface-900 dark:text-surface-0">Wachtwoord herstellen</h3>
          <p class="text-surface-500 m-0">Kies een nieuw wachtwoord voor je account.</p>
        </div>

        <form [formGroup]="resetPasswordForm" (ngSubmit)="onSubmit()">
          <div class="field">
            <label for="email" class="block">{{ lang.t().email }}</label>
            <input id="email" type="email" pInputText formControlName="email" class="w-full" autocomplete="email" />
            @if (resetPasswordForm.controls.email.invalid && resetPasswordForm.controls.email.touched) {
              <small class="p-error block mt-1">{{ lang.t().invalid_email }}</small>
            }
          </div>

          <div class="field mt-4">
            <label for="newPassword" class="block">{{ lang.t().new_password }}</label>
            <p-password id="newPassword" formControlName="newPassword" [feedback]="true" [toggleMask]="true" styleClass="w-full" inputStyleClass="w-full" autocomplete="new-password"></p-password>
            @if (resetPasswordForm.controls.newPassword.invalid && resetPasswordForm.controls.newPassword.touched) {
              <small class="p-error block mt-1">{{ lang.t().password_required }}</small>
            }
          </div>

          <div class="field mt-4">
            <label for="confirmPassword" class="block">Nieuw wachtwoord bevestigen</label>
            <p-password id="confirmPassword" formControlName="confirmPassword" [feedback]="false" [toggleMask]="true" styleClass="w-full" inputStyleClass="w-full" autocomplete="new-password"></p-password>
            @if (resetPasswordForm.hasError('passwordMismatch') && resetPasswordForm.controls.confirmPassword.touched) {
              <small class="p-error block mt-1">De wachtwoorden komen niet overeen.</small>
            }
          </div>

          @if (successMessage()) {
            <p-message severity="success" [text]="successMessage()" styleClass="w-full mt-4"></p-message>
          }

          @if (errorMessage()) {
            <p-message severity="error" [text]="errorMessage()" styleClass="w-full mt-4"></p-message>
          }

          <div class="mt-4 flex gap-2">
            <p-button label="Wachtwoord herstellen" type="submit" [disabled]="resetPasswordForm.invalid || isLoading() || !resetCode()" [loading]="isLoading()" styleClass="flex-1"></p-button>
            <p-button [label]="lang.t().login" routerLink="/login" severity="secondary" styleClass="flex-1"></p-button>
          </div>
        </form>
      </p-card>
    </div>
  `,
  styles: [`
    .auth-wrapper {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background-color: var(--surface-ground);
      padding: 2rem 1rem;
    }
    .auth-card {
      width: 100%;
      max-width: 420px;
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
    .flex {
      display: flex;
    }
    .gap-2 {
      gap: 0.5rem;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class ResetPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  public readonly lang = inject(LanguageService);

  resetCode = signal(this.route.snapshot.queryParamMap.get('resetCode') ?? this.route.snapshot.queryParamMap.get('code') ?? '');

  resetPasswordForm = this.fb.nonNullable.group({
    email: [this.route.snapshot.queryParamMap.get('email') ?? '', [Validators.required, Validators.email]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', Validators.required]
  }, { validators: ResetPasswordComponent.passwordsMatch });

  isLoading = signal(false);
  successMessage = signal('');
  errorMessage = signal(this.resetCode() ? '' : 'Deze herstellink mist een resetcode. Vraag een nieuwe herstellink aan.');

  onSubmit(): void {
    if (this.resetPasswordForm.invalid || !this.resetCode()) {
      this.resetPasswordForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    const value = this.resetPasswordForm.getRawValue();
    this.http.post('/api/v1/identity/resetPassword', {
      email: value.email,
      resetCode: this.resetCode(),
      newPassword: value.newPassword
    }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.successMessage.set('Je wachtwoord is bijgewerkt. Je wordt doorgestuurd naar de login.');
        setTimeout(() => this.router.navigate(['/login']), 1800);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.errorMessage.set(this.firstValidationError(err) ?? 'Je wachtwoord kon niet worden bijgewerkt. Controleer de link en probeer het opnieuw.');
      }
    });
  }

  private static passwordsMatch(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    return newPassword && confirmPassword && newPassword !== confirmPassword
      ? { passwordMismatch: true }
      : null;
  }

  private firstValidationError(err: any): string | null {
    const errors = err.error?.errors;
    if (!errors) {
      return null;
    }

    const firstErrorKey = Object.keys(errors)[0];
    return errors[firstErrorKey]?.[0] ?? null;
  }
}
