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
    DividerModule
  ],
  template: `
    <div class="flex flex-col gap-6 max-w-4xl mx-auto">
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
  globalSettingsForm = this.fb.nonNullable.group({
    whatsNewContent: ['', Validators.required],
    googleMapsApiKey: [''],
    googleMapsMapId: ['']
  });

  ngOnInit(): void {
    this.loadContent();
  }

  loadContent(): void {
    forkJoin({
      whatsNew: this.settingsService.getSetting('WhatsNewContent').pipe(catchError(() => of({ value: '' }))),
      mapsKey: this.settingsService.getSetting('GoogleMapsApiKey').pipe(catchError(() => of({ value: '' }))),
      mapId: this.settingsService.getSetting('GoogleMapsMapId').pipe(catchError(() => of({ value: '' })))
    }).subscribe({
      next: (results) => {
        this.globalSettingsForm.patchValue({ 
          whatsNewContent: results.whatsNew.value,
          googleMapsApiKey: results.mapsKey.value,
          googleMapsMapId: results.mapId.value
        });
        this.globalSettingsForm.markAsPristine();
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
      }
    });
  }

  save(): void {
    if (this.globalSettingsForm.invalid) return;

    this.saving.set(true);
    const formValue = this.globalSettingsForm.getRawValue();

    const updates = [];
    if (this.globalSettingsForm.get('whatsNewContent')?.dirty) {
      updates.push(this.settingsService.updateSetting('WhatsNewContent', { value: formValue.whatsNewContent }));
    }
    if (this.globalSettingsForm.get('googleMapsApiKey')?.dirty) {
      updates.push(this.settingsService.updateSetting('GoogleMapsApiKey', { value: formValue.googleMapsApiKey }));
    }
    if (this.globalSettingsForm.get('googleMapsMapId')?.dirty) {
      updates.push(this.settingsService.updateSetting('GoogleMapsMapId', { value: formValue.googleMapsMapId }));
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
