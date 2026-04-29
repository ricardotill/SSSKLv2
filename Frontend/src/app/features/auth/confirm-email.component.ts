import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageModule } from 'primeng/message';
import { LanguageService } from '../../core/services/language.service';
import { BrandingComponent } from '../../shared/components/branding/branding.component';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [ButtonModule, CardModule, MessageModule, RouterModule, BrandingComponent],
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
          <h3 class="text-xl font-medium mt-0 mb-3 text-surface-900 dark:text-surface-0">E-mailadres bevestigen</h3>
          <p class="text-surface-500 m-0">{{ statusText() }}</p>
        </div>

        @if (isLoading()) {
          <p-message severity="info" [text]="lang.t().loading" styleClass="w-full"></p-message>
        }

        @if (successMessage()) {
          <p-message severity="success" [text]="successMessage()" styleClass="w-full"></p-message>
        }

        @if (errorMessage()) {
          <p-message severity="error" [text]="errorMessage()" styleClass="w-full"></p-message>
        }

        <div class="mt-4 flex gap-2">
          <p-button [label]="lang.t().login" routerLink="/login" styleClass="flex-1"></p-button>
          @if (!successMessage() && !isLoading()) {
            <p-button label="Bevestigingsmail opnieuw versturen" routerLink="/resend-confirmation-email" severity="secondary" styleClass="flex-1"></p-button>
          }
        </div>
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
    .mt-4 {
      margin-top: 1rem;
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
export default class ConfirmEmailComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  public readonly lang = inject(LanguageService);

  isLoading = signal(true);
  successMessage = signal('');
  errorMessage = signal('');
  statusText = signal('');

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const userId = params.get('userId');
    const code = params.get('code');
    const changedEmail = params.get('changedEmail');

    if (!userId || !code) {
      this.isLoading.set(false);
      this.statusText.set('Deze bevestigingslink mist gegevens.');
      this.errorMessage.set('E-mailadres bevestigen mislukt.');
      return;
    }

    this.statusText.set('We bevestigen je e-mailadres.');
    const requestParams: Record<string, string> = { userId, code };
    if (changedEmail) {
      requestParams['changedEmail'] = changedEmail;
    }

    this.http.get('/api/v1/identity/confirmEmail', { params: requestParams, responseType: 'text' }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.statusText.set('Je e-mailadres is bevestigd.');
        this.successMessage.set('Je account is klaar voor gebruik. Je kunt nu inloggen.');
      },
      error: () => {
        this.isLoading.set(false);
        this.statusText.set('E-mailadres bevestigen mislukt.');
        this.errorMessage.set('De bevestigingslink is ongeldig of verlopen. Vraag een nieuwe bevestigingsmail aan.');
      }
    });
  }
}
