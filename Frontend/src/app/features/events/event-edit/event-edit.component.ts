import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';
import { DatePickerModule } from 'primeng/datepicker';
import { EventService } from '../../../core/services/event.service';
import { LanguageService } from '../../../core/services/language.service';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { FileUploadModule } from 'primeng/fileupload';

@Component({
  selector: 'app-event-edit',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    EditorModule,
    DatePickerModule,
    ToastModule,
    ProgressSpinnerModule,
    FileUploadModule
  ],
  providers: [MessageService],
  template: `
    <div class="max-w-3xl mx-auto">
      <p-toast></p-toast>
      
      <div class="flex items-center gap-3 mb-6">
        <p-button icon="pi pi-arrow-left" [text]="true" severity="secondary" [routerLink]="isEdit ? ['/events', eventId] : ['/events']" />
        <h1 class="text-3xl font-bold m-0 text-surface-900 dark:text-surface-0">
          {{ isEdit ? ls.t().edit_event : ls.t().create_event }}
        </h1>
      </div>

      <p-card>
        <form [formGroup]="eventForm" (ngSubmit)="onSubmit()" class="flex flex-col gap-6">
          <div class="flex flex-col gap-2">
            <label for="title" class="font-bold">{{ ls.t().title }}</label>
            <input pInputText id="title" formControlName="title" [placeholder]="ls.t().title" class="w-full" />
            @if (eventForm.get('title')?.touched && eventForm.get('title')?.invalid) {
              <small class="text-red-500">Titel is verplicht</small>
            }
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="flex flex-col gap-2">
              <label for="startDateTime" class="font-bold">{{ ls.t().start_date }}</label>
              <p-datepicker 
                id="startDateTime" 
                formControlName="startDateTime" 
                [showTime]="true" 
                [showIcon]="true" 
                appendTo="body"
                class="w-full"
              ></p-datepicker>
            </div>
            <div class="flex flex-col gap-2">
              <label for="endDateTime" class="font-bold">{{ ls.t().end_date }}</label>
              <p-datepicker 
                id="endDateTime" 
                formControlName="endDateTime" 
                [showTime]="true" 
                [showIcon]="true" 
                appendTo="body"
                class="w-full"
              ></p-datepicker>
            </div>
          </div>

          <div class="flex flex-col gap-2">
            <label for="image" class="font-bold">Afbeelding (.png, .jpg)</label>
            @if (isEdit && currentImageUri()) {
              <div class="mb-2">
                <span class="text-sm text-surface-500 block mb-1">Huidige afbeelding:</span>
                <img [src]="currentImageUri()" class="w-16 h-16 object-contain rounded-md border border-surface-200 dark:border-surface-700 bg-surface-100 dark:bg-surface-800" />
              </div>
            }
            <input type="file" id="image" (change)="onFileSelected($event)" accept="image/png, image/jpeg" class="w-full p-2 border border-surface-200 dark:border-surface-700 rounded-md" />
          </div>

          <div class="flex flex-col gap-2">
            <label class="font-bold">{{ ls.t().description }}</label>
            <p-editor formControlName="description" [style]="{ height: '320px' }">
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

          <div class="flex justify-end gap-3 mt-4">
            <p-button 
              [label]="ls.t().cancel" 
              severity="secondary" 
              [outlined]="true" 
              [routerLink]="isEdit ? ['/events', eventId] : ['/events']" 
            />
            <p-button 
              [label]="ls.t().save" 
              type="submit" 
              severity="primary" 
              [loading]="submitting()"
              [disabled]="eventForm.invalid"
            />
          </div>
        </form>
      </p-card>
    </div>
  `,
  styles: [],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export default class EventEditComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly eventService = inject(EventService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  ls = inject(LanguageService);

  eventForm: FormGroup;
  isEdit = false;
  eventId: string | null = null;
  submitting = signal<boolean>(false);
  currentImageUri = signal<string | null>(null);
  selectedFile: File | null = null;

  constructor() {
    this.eventForm = this.fb.group({
      title: ['', Validators.required],
      description: ['', Validators.required],
      startDateTime: [null, Validators.required],
      endDateTime: [null, Validators.required]
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.eventId = id;
      this.isEdit = true;
      this.loadEvent(id);
    }
  }

  loadEvent(id: string): void {
    this.eventService.getEvent(id).subscribe({
      next: (event) => {
        this.currentImageUri.set(event.imageUrl || null);
        this.eventForm.patchValue({
          title: event.title,
          description: event.description,
          startDateTime: new Date(event.startDateTime),
          endDateTime: new Date(event.endDateTime)
        });
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Kan evenement niet laden' })
    });
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
    }
  }

  onSubmit(): void {
    if (this.eventForm.invalid) return;

    this.submitting.set(true);
    const formValue = this.eventForm.value;
    
    const formData = new FormData();
    formData.append('Title', formValue.title);
    formData.append('Description', formValue.description);
    formData.append('StartDateTime', formValue.startDateTime.toISOString());
    formData.append('EndDateTime', formValue.endDateTime.toISOString());

    if (this.selectedFile) {
      formData.append('image', this.selectedFile);
    }

    const request: Observable<any> = this.isEdit && this.eventId
      ? this.eventService.updateEvent(this.eventId, formData)
      : this.eventService.createEvent(formData);

    request.subscribe({
      next: () => {
        this.submitting.set(false);
        this.messageService.add({ severity: 'success', summary: 'Succes', detail: 'Evenement opgeslagen' });
        setTimeout(() => this.router.navigate(['/events']), 1000);
      },
      error: () => {
        this.submitting.set(false);
        this.messageService.add({ severity: 'error', summary: 'Fout', detail: 'Opslaan mislukt' });
      }
    });
  }
}
