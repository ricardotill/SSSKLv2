import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EditorModule } from 'primeng/editor';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { DividerModule } from 'primeng/divider';
import { MessageService } from 'primeng/api';
import { GlobalSettingsService } from '../services/global-settings.service';
import { LanguageService } from '../../../core/services/language.service';
import { MultiSelectModule } from 'primeng/multiselect';
import { RoleService } from '../../admin/services/role.service';
import { Role } from '../../../core/models/role.model';
import { finalize, forkJoin, of, catchError } from 'rxjs';

@Component({
  selector: 'app-admin-global-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    EditorModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    PasswordModule,
    DividerModule,
    MultiSelectModule
  ],
  template: `
    <div class="flex flex-col gap-6 max-w-4xl mx-auto pb-12">
      <div class="flex justify-between items-center">
        <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().global_settings_admin }}</h1>
      </div>

      <form [formGroup]="globalSettingsForm" (ngSubmit)="save()" class="flex flex-col gap-6">
        <!-- API Configuration -->
        <p-card header="API Configuratie" subheader="Configureer externe API details hier.">
          <div class="flex flex-col gap-4">
            <div class="flex flex-col gap-2">
              <label for="googleMapsApiKey" class="font-bold">Google Maps API Key</label>
              <p-password 
                id="googleMapsApiKey" 
                formControlName="googleMapsApiKey" 
                [toggleMask]="true" 
                [feedback]="false" 
                styleClass="w-full" 
                inputStyleClass="w-full"
                [placeholder]="'AIza...'"
              ></p-password>
              <small class="text-surface-500">Deze sleutel wordt gebruikt voor de locatiezoeker en kaartweergave.</small>
            </div>

            <div class="flex flex-col gap-2">
              <label for="googleMapsMapId" class="font-bold">Google Maps Map ID</label>
              <input 
                pInputText
                id="googleMapsMapId" 
                formControlName="googleMapsMapId" 
                class="w-full" 
                [placeholder]="'8e0a97...'"
              />
              <small class="text-surface-500">Vereist voor moderne "Advanced Markers". Maak deze aan in de Google Maps Console.</small>
            </div>
          </div>
        </p-card>

        <!-- Email Configuration -->
        <p-card header="Email Configuratie (SMTP)" subheader="Configureer de SMTP server voor het versturen van emails.">
          <div class="flex flex-col gap-4">
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div class="flex flex-col gap-2">
                <label for="emailSmtpServer" class="font-bold">SMTP Server</label>
                <input pInputText id="emailSmtpServer" formControlName="emailSmtpServer" placeholder="smtp.gmail.com" />
              </div>
              <div class="flex flex-col gap-2">
                <label for="emailSmtpPort" class="font-bold">SMTP Poort</label>
                <input pInputText id="emailSmtpPort" formControlName="emailSmtpPort" placeholder="587" />
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div class="flex flex-col gap-2">
                <label for="emailSmtpUsername" class="font-bold">Gebruikersnaam / Email</label>
                <input pInputText id="emailSmtpUsername" formControlName="emailSmtpUsername" placeholder="info@domain.com" />
              </div>
              <div class="flex flex-col gap-2">
                <label for="emailSmtpPassword" class="font-bold">Wachtwoord / App Password</label>
                <p-password 
                  id="emailSmtpPassword" 
                  formControlName="emailSmtpPassword" 
                  [toggleMask]="true" 
                  [feedback]="false" 
                  styleClass="w-full" 
                  inputStyleClass="w-full"
                  [placeholder]="passwordIsSet() ? '●●●●●●●● (reeds ingesteld)' : 'App wachtwoord invoeren'"
                ></p-password>
                @if (passwordIsSet() && !globalSettingsForm.controls.emailSmtpPassword.dirty) {
                  <small class="text-emerald-600 flex items-center gap-1"><i class="pi pi-lock"></i> Wachtwoord is ingesteld. Laat leeg om het huidige wachtwoord te behouden.</small>
                }
              </div>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div class="flex flex-col gap-2">
                <label for="emailSenderEmail" class="font-bold">Afzender Email</label>
                <input pInputText id="emailSenderEmail" formControlName="emailSenderEmail" placeholder="no-reply@domain.com" />
              </div>
              <div class="flex flex-col gap-2">
                <label for="emailSenderName" class="font-bold">Afzender Naam</label>
                <input pInputText id="emailSenderName" formControlName="emailSenderName" placeholder="Scouting Wilo" />
              </div>
            </div>

            <p-divider></p-divider>
            
            <div class="flex items-center justify-between">
              <span class="text-surface-600 text-sm">Sla de instellingen eerst op voordat je een test-email verstuurt.</span>
              <p-button 
                label="Verstuur Test Email" 
                icon="pi pi-envelope" 
                severity="secondary" 
                (onClick)="testEmail()" 
                [loading]="sendingTestEmail()" />
            </div>
          </div>
        </p-card>
        
        <!-- Quotes Feature Configuration -->
        <p-card header="Quotes Feature" subheader="Configureer wie de Quotes feature mag gebruiken.">
          <div class="flex flex-col gap-2">
            <label for="quotesRoles" class="font-bold">Toegestane Rollen</label>
            <p-multiselect 
              id="quotesRoles" 
              formControlName="quotesAllowedRoles" 
              [options]="availableRoles()" 
              optionLabel="name" 
              optionValue="name" 
              [placeholder]="'Selecteer rollen'" 
              display="chip"
              class="w-full">
            </p-multiselect>
            <small class="text-surface-500">Laat leeg om iedereen toegang te geven. De controller heeft zijn eigen check.</small>
          </div>
        </p-card>

        <!-- What's New Content -->
        <p-card [header]="ls.t().whats_new_settings_section_title" [subheader]="ls.t().whats_new_settings_section_desc">
          <div class="flex flex-col gap-2">
            <p-editor formControlName="whatsNewContent" [style]="{ height: '400px' }">
              <ng-template pTemplate="header">
                <span class="ql-formats">
                  <button type="button" class="ql-bold" aria-label="Bold"></button>
                  <button type="button" class="ql-italic" aria-label="Italic"></button>
                  <button type="button" class="ql-underline" aria-label="Underline"></button>
                </span>
                <span class="ql-formats">
                  <button type="button" class="ql-list" value="ordered" aria-label="Ordered List"></button>
                  <button type="button" class="ql-list" value="bullet" aria-label="Bullet List"></button>
                </span>
                <span class="ql-formats">
                  <button type="button" class="ql-link" aria-label="Insert Link"></button>
                </span>
              </ng-template>
            </p-editor>
          </div>
        </p-card>

        <div class="flex justify-end sticky bottom-4 z-10">
          <p-button 
            [label]="ls.t().save" 
            icon="pi pi-check" 
            type="submit" 
            [loading]="saving()" 
            [disabled]="globalSettingsForm.invalid || !globalSettingsForm.dirty" />
        </div>
      </form>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class AdminGlobalSettingsComponent implements OnInit {
  private readonly settingsService = inject(GlobalSettingsService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  ls = inject(LanguageService);

  saving = signal<boolean>(false);
  sendingTestEmail = signal<boolean>(false);
  passwordIsSet = signal<boolean>(false);

  globalSettingsForm = this.fb.nonNullable.group({
    whatsNewContent: ['', Validators.required],
    googleMapsApiKey: [''],
    googleMapsMapId: [''],
    quotesAllowedRoles: [[] as string[]],
    emailSmtpServer: [''],
    emailSmtpPort: [''],
    emailSmtpUsername: [''],
    emailSmtpPassword: [''],
    emailSenderEmail: [''],
    emailSenderName: ['']
  });

  availableRoles = signal<Role[]>([]);
  private readonly roleService = inject(RoleService);

  ngOnInit(): void {
    this.loadContent();
    this.loadRoles();
  }

  loadRoles(): void {
    this.roleService.getAllRoles().subscribe({
      next: (roles) => this.availableRoles.set(roles),
      error: () => console.error('Failed to load roles')
    });
  }

  loadContent(): void {
    forkJoin({
      whatsNew: this.settingsService.getSetting('WhatsNewContent').pipe(catchError(() => of({ value: '' }))),
      mapsKey: this.settingsService.getSetting('GoogleMapsApiKey').pipe(catchError(() => of({ value: '' }))),
      mapId: this.settingsService.getSetting('GoogleMapsMapId').pipe(catchError(() => of({ value: '' }))),
      quotesRoles: this.settingsService.getSetting('QuotesFeatureAllowedRoles').pipe(catchError(() => of({ value: '' }))),
      emailServer: this.settingsService.getSetting('EmailSmtpServer').pipe(catchError(() => of({ value: '' }))),
      emailPort: this.settingsService.getSetting('EmailSmtpPort').pipe(catchError(() => of({ value: '' }))),
      emailUser: this.settingsService.getSetting('EmailSmtpUsername').pipe(catchError(() => of({ value: '' }))),
      // EmailSmtpPassword is write-only: we check if it exists but never load its value
      emailPassExists: this.settingsService.settingExists('EmailSmtpPassword'),
      emailFrom: this.settingsService.getSetting('EmailSenderEmail').pipe(catchError(() => of({ value: '' }))),
      emailFromName: this.settingsService.getSetting('EmailSenderName').pipe(catchError(() => of({ value: '' })))
    }).subscribe({
      next: (results) => {
        this.passwordIsSet.set(results.emailPassExists);
        this.globalSettingsForm.patchValue({ 
          whatsNewContent: results.whatsNew.value,
          googleMapsApiKey: results.mapsKey.value,
          googleMapsMapId: results.mapId.value,
          quotesAllowedRoles: results.quotesRoles.value ? results.quotesRoles.value.split(',').map(r => r.trim()) : [],
          emailSmtpServer: results.emailServer.value,
          emailSmtpPort: results.emailPort.value,
          emailSmtpUsername: results.emailUser.value,
          // password intentionally not patched — never retrieved from API
          emailSenderEmail: results.emailFrom.value,
          emailSenderName: results.emailFromName.value
        });
        this.globalSettingsForm.markAsPristine();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
      }
    });
  }

  testEmail(): void {
    if (this.globalSettingsForm.dirty) {
      this.messageService.add({ severity: 'warn', summary: 'Sla eerst op', detail: 'Sla je wijzigingen op voordat je test.' });
      return;
    }

    this.sendingTestEmail.set(true);
    this.settingsService.sendTestEmail().pipe(
      finalize(() => this.sendingTestEmail.set(false))
    ).subscribe({
      next: (res) => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: res.message });
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Fout', detail: err.error?.message || 'Kon test email niet versturen' });
      }
    });
  }

  save(): void {
    if (this.globalSettingsForm.invalid) return;

    this.saving.set(true);
    const formValue = this.globalSettingsForm.getRawValue();

    const updates = [];
    const controls = this.globalSettingsForm.controls;

    if (controls.whatsNewContent.dirty) {
      updates.push(this.settingsService.updateSetting('WhatsNewContent', { value: formValue.whatsNewContent }));
    }
    if (controls.googleMapsApiKey.dirty) {
      updates.push(this.settingsService.updateSetting('GoogleMapsApiKey', { value: formValue.googleMapsApiKey }));
    }
    if (controls.googleMapsMapId.dirty) {
      updates.push(this.settingsService.updateSetting('GoogleMapsMapId', { value: formValue.googleMapsMapId }));
    }
    if (controls.quotesAllowedRoles.dirty) {
      updates.push(this.settingsService.updateSetting('QuotesFeatureAllowedRoles', { value: formValue.quotesAllowedRoles.join(',') }));
    }
    if (controls.emailSmtpServer.dirty) {
      updates.push(this.settingsService.updateSetting('EmailSmtpServer', { value: formValue.emailSmtpServer }));
    }
    if (controls.emailSmtpPort.dirty) {
      updates.push(this.settingsService.updateSetting('EmailSmtpPort', { value: formValue.emailSmtpPort }));
    }
    if (controls.emailSmtpUsername.dirty) {
      updates.push(this.settingsService.updateSetting('EmailSmtpUsername', { value: formValue.emailSmtpUsername }));
    }
    if (controls.emailSmtpPassword.dirty && formValue.emailSmtpPassword.trim() !== '') {
      updates.push(this.settingsService.updateSetting('EmailSmtpPassword', { value: formValue.emailSmtpPassword }));
    }
    if (controls.emailSenderEmail.dirty) {
      updates.push(this.settingsService.updateSetting('EmailSenderEmail', { value: formValue.emailSenderEmail }));
    }
    if (controls.emailSenderName.dirty) {
      updates.push(this.settingsService.updateSetting('EmailSenderName', { value: formValue.emailSenderName }));
    }

    if (updates.length === 0) {
      this.saving.set(false);
      return;
    }

    forkJoin(updates)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().global_settings_updated });
          this.globalSettingsForm.markAsPristine();
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: 'Opslaan mislukt' });
        }
      });
  }
}
