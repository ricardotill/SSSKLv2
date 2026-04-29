import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { LanguageService } from '../../core/services/language.service';
import { BrandingComponent } from '../../shared/components/branding/branding.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, InputTextModule, ButtonModule, CardModule, MessageModule, RouterModule, BrandingComponent],
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
          <h3 class="text-xl font-medium mt-0 mb-3 text-surface-900 dark:text-surface-0">Wachtwoord vergeten?</h3>
          <p class="text-surface-500 m-0">Vul je e-mailadres in, dan sturen we een link om je wachtwoord opnieuw in te stellen.</p>
        </div>

        <form [formGroup]="forgotPasswordForm" (ngSubmit)="onSubmit()">
          <div class="field">
            <label for="email" class="block">{{ lang.t().email }}</label>
            <input id="email" type="email" pInputText formControlName="email" class="w-full" autocomplete="email" />
            @if (forgotPasswordForm.controls.email.invalid && forgotPasswordForm.controls.email.touched) {
              <small class="p-error block mt-1">{{ lang.t().invalid_email }}</small>
            }
          </div>

          @if (successMessage()) {
            <p-message severity="success" [text]="successMessage()" styleClass="w-full mt-4"></p-message>
          }

          @if (errorMessage()) {
            <p-message severity="error" [text]="errorMessage()" styleClass="w-full mt-4"></p-message>
          }

          <div class="mt-4">
            <p-button label="Herstellink versturen" type="submit" [disabled]="forgotPasswordForm.invalid || isLoading()" [loading]="isLoading()" styleClass="w-full"></p-button>
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
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly http = inject(HttpClient);
  public readonly lang = inject(LanguageService);

  forgotPasswordForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  isLoading = signal(false);
  successMessage = signal('');
  errorMessage = signal('');

  onSubmit(): void {
    if (this.forgotPasswordForm.invalid) {
      this.forgotPasswordForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    this.http.post('/api/v1/identity/forgotPassword', this.forgotPasswordForm.getRawValue()).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.successMessage.set('Als dit e-mailadres bij een bevestigd account hoort, is er een herstellink verstuurd.');
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('De herstellink kon niet worden verstuurd. Probeer het later opnieuw.');
      }
    });
  }
}
