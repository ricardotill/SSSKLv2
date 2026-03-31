import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EditorModule } from 'primeng/editor';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageService } from 'primeng/api';
import { GlobalSettingsService } from '../services/global-settings.service';
import { LanguageService } from '../../../core/services/language.service';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-admin-global-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    EditorModule,
    ButtonModule,
    CardModule
  ],
  template: `
    <div class="flex justify-between items-center mb-4">
      <h1 class="text-2xl font-bold m-0 text-surface-900 dark:text-surface-0">{{ ls.t().global_settings_admin }}</h1>
    </div>

    <p-card [header]="ls.t().whats_new_settings_section_title" [subheader]="ls.t().whats_new_settings_section_desc">
      <form [formGroup]="globalSettingsForm" (ngSubmit)="save()" class="flex flex-col gap-4">
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

        <div class="flex justify-end mt-4">
          <p-button 
            [label]="ls.t().save" 
            icon="pi pi-check" 
            type="submit" 
            [loading]="saving()" 
            [disabled]="globalSettingsForm.invalid" />
        </div>
      </form>
    </p-card>
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
    whatsNewContent: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadContent();
  }

  loadContent(): void {
    this.settingsService.getSetting('WhatsNewContent').subscribe({
      next: (setting) => {
        this.globalSettingsForm.patchValue({ whatsNewContent: setting.value });
      },
      error: (err) => {
        if (err.status !== 404) {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().load_failed });
        }
      }
    });
  }

  save(): void {
    if (this.globalSettingsForm.invalid) return;

    this.saving.set(true);
    const content = this.globalSettingsForm.getRawValue().whatsNewContent;

    this.settingsService.updateSetting('WhatsNewContent', { value: content })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: this.ls.t().success, detail: this.ls.t().global_settings_updated });
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: this.ls.t().error, detail: this.ls.t().error });
        }
      });
  }
}
