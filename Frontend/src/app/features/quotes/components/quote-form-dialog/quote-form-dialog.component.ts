import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { MultiSelectModule } from 'primeng/multiselect';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { QuoteDto, QuoteCreateDto, QuoteUpdateDto } from '../../../../core/models/quote.model';
import { ApplicationUserService } from '../../../../features/users/services/application-user.service';
import { ApplicationUserDto, PaginatedUsers } from '../../../../core/models/application-user.model';
import { RoleService } from '../../../admin/services/role.service';
import { Role } from '../../../../core/models/role.model';
import { LanguageService } from '../../../../core/services/language.service';
import { ChipModule } from 'primeng/chip';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { GlobalSettingsService } from '../../../admin/services/global-settings.service';
import { catchError, of } from 'rxjs';

interface AuthorOption {
  id?: string;
  name: string;
  isUser: boolean;
  user?: ApplicationUserDto;
}

@Component({
  selector: 'app-quote-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    DatePickerModule,
    MultiSelectModule,
    AutoCompleteModule,
    ChipModule,
    ToggleSwitchModule
  ],
  template: `
    <form [formGroup]="quoteForm" (ngSubmit)="onSubmit()" class="flex flex-col gap-5 mt-2">
      <!-- Quote Text -->
      <div class="flex flex-col gap-2">
        <label for="text" class="font-bold text-surface-700 dark:text-surface-300">{{ ls.t().quote_text }}</label>
        <textarea 
          pTextarea 
          id="text" 
          formControlName="text" 
          [rows]="3" 
          [autoResize]="true"
          class="w-full"
          [placeholder]="ls.t().quote_text"
        ></textarea>
        @if (quoteForm.get('text')?.touched && quoteForm.get('text')?.invalid) {
          <small class="text-red-500">{{ ls.t().quote_text_required }}</small>
        }
      </div>

      <!-- Date Said -->
      <div class="flex flex-col gap-2">
        <label for="dateSaid" class="font-bold text-surface-700 dark:text-surface-300">{{ ls.t().when_was_it_said }}</label>
        <p-datepicker 
          id="dateSaid" 
          formControlName="dateSaid" 
          [showIcon]="true" 
          appendTo="body"
          class="w-full"
          dateFormat="dd/mm/yy"
        ></p-datepicker>
      </div>

      <!-- Authors Selection -->
      <div class="flex flex-col gap-2">
        <label for="authors" class="font-bold text-surface-700 dark:text-surface-300">{{ ls.t().who_said_it }}</label>
        <div class="flex flex-wrap gap-2 mb-2">
          @for (author of selectedAuthors(); track $index) {
            <p-chip 
              [label]="author.name" 
              [removable]="true" 
              (onRemove)="removeAuthor(author)"
              [icon]="author.isUser ? 'pi pi-user' : 'pi pi-pencil'"
            ></p-chip>
          }
        </div>
        <p-autocomplete
          [(ngModel)]="authorInput"
          [ngModelOptions]="{standalone: true}"
          [suggestions]="filteredAuthors()"
          (completeMethod)="searchAuthors($event)"
          (onSelect)="addAuthor($event)"
          optionLabel="name"
          dataKey="id"
          [placeholder]="ls.t().select_user_or_custom"
          class="w-full"
          appendTo="body"
          [dropdown]="true"
        >
          <ng-template let-option pTemplate="item">
            <div class="flex items-center gap-2">
              @if (option.isUser) {
                <i class="pi pi-user text-primary-500"></i>
                <span>{{ option.name }}</span>
              } @else if (option.isNew) {
                <i class="pi pi-plus text-green-500"></i>
                <span>{{ ls.translate('add_custom_name', { name: option.name }) }}</span>
              }
            </div>
          </ng-template>
        </p-autocomplete>
        @if (selectedAuthors().length === 0 && quoteForm.get('authors')?.touched) {
          <small class="text-red-500">{{ ls.t().authors_required }}</small>
        }
      </div>

      <!-- Visible To Roles -->
      <div class="flex flex-col gap-2">
        <label for="visibleToRoles" class="font-bold text-surface-700 dark:text-surface-300">{{ ls.t().visible_to_roles }}</label>
        <p-multiselect 
          id="visibleToRoles" 
          formControlName="visibleToRoles" 
          [options]="availableRoles()" 
          optionLabel="name" 
          optionValue="name" 
          [placeholder]="ls.t().select_roles" 
          display="chip"
          class="w-full"
          appendTo="body"
        ></p-multiselect>
        <small class="text-surface-500">{{ ls.t().visible_to_roles_help }}</small>
      </div>

      <!-- Notification Toggle (Only for new quotes) -->
      @if (!isEdit) {
        <div class="flex items-center justify-between p-3 bg-surface-50 dark:bg-surface-800/50 rounded-lg border border-surface-200 dark:border-surface-700">
          <div class="flex flex-col gap-1">
            <span class="font-bold text-surface-700 dark:text-surface-300">{{ ls.t().send_notification_toggle }}</span>
            <small class="text-surface-500">{{ ls.t().send_notification_help }}</small>
          </div>
          <p-toggleswitch formControlName="sendNotification"></p-toggleswitch>
        </div>
      }

      <!-- Action Buttons -->
      <div class="flex justify-end gap-3 mt-4">
        <p-button [label]="ls.t().cancel" severity="secondary" [outlined]="true" (onClick)="onCancel()" />
        <p-button 
          [label]="ls.t().save" 
          type="submit" 
          severity="primary" 
          [loading]="submitting()"
          [disabled]="quoteForm.invalid || selectedAuthors().length === 0"
        />
      </div>
    </form>
  `
})
export class QuoteFormDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(ApplicationUserService);
  private readonly roleService = inject(RoleService);
  private readonly ref = inject(DynamicDialogRef);
  private readonly config = inject(DynamicDialogConfig);
  private readonly settingsService = inject(GlobalSettingsService);
  ls = inject(LanguageService);

  quoteForm: FormGroup;
  isEdit = false;
  quoteId: string | null = null;
  submitting = signal<boolean>(false);
  
  availableUsers = signal<ApplicationUserDto[]>([]);
  availableRoles = signal<Role[]>([]);
  
  // Author management
  authorInput = '';
  selectedAuthors = signal<AuthorOption[]>([]);
  filteredAuthors = signal<any[]>([]);

  constructor() {
    this.quoteForm = this.fb.group({
      text: ['', Validators.required],
      dateSaid: [new Date(), Validators.required],
      visibleToRoles: [[]],
      sendNotification: [false]
    });
  }

  ngOnInit(): void {
    this.loadData();
    
    if (this.config.data?.quote) {
      const quote = this.config.data.quote as QuoteDto;
      this.isEdit = true;
      this.quoteId = quote.id;
      
      this.quoteForm.patchValue({
        text: quote.text,
        dateSaid: new Date(quote.dateSaid),
        visibleToRoles: quote.visibleToRoles
      });
      
      const authors: AuthorOption[] = quote.authors.map(a => ({
        id: a.applicationUserId || undefined,
        name: a.customName || a.applicationUser?.fullName || 'Unknown',
        isUser: !!a.applicationUserId,
        user: a.applicationUser || undefined
      }));
      this.selectedAuthors.set(authors);
    }
  }

  loadData(): void {
    this.userService.getUsers().subscribe((res: PaginatedUsers) => this.availableUsers.set(res.items));
    
    // Load all roles and filter by global settings
    this.roleService.getAllRoles().subscribe(allRoles => {
      this.settingsService.getSetting('QuotesFeatureAllowedRoles').pipe(
        catchError(() => of({ value: '' }))
      ).subscribe(setting => {
        if (setting && setting.value) {
          const allowedRoleNames = setting.value.split(',').map((r: string) => r.trim());
          this.availableRoles.set(allRoles.filter(r => allowedRoleNames.includes(r.name)));
        } else {
          // If no setting, allow all roles
          this.availableRoles.set(allRoles);
        }
      });
    });
  }

  searchAuthors(event: AutoCompleteCompleteEvent): void {
    const query = event.query.toLowerCase();
    const suggestions: any[] = this.availableUsers()
      .filter(u => u.fullName.toLowerCase().includes(query) || u.userName.toLowerCase().includes(query))
      .map(u => ({
        id: u.id,
        name: u.fullName,
        isUser: true,
        user: u
      }));
    
    if (query && !suggestions.some(s => s.name.toLowerCase() === query)) {
      suggestions.push({
        name: event.query,
        isUser: false,
        isNew: true
      });
    }
    
    this.filteredAuthors.set(suggestions);
  }

  addAuthor(event: any): void {
    const author = event.value as any;
    if (this.selectedAuthors().some(a => a.name === author.name)) {
      this.authorInput = '';
      return;
    }
    
    this.selectedAuthors.update(list => [...list, {
      id: author.id,
      name: author.name,
      isUser: author.isUser,
      user: author.user
    }]);
    
    this.authorInput = '';
  }

  removeAuthor(author: AuthorOption): void {
    this.selectedAuthors.update(list => list.filter(a => a !== author));
  }

  onCancel(): void {
    this.ref.close();
  }

  onSubmit(): void {
    if (this.quoteForm.invalid || this.selectedAuthors().length === 0) return;

    const formValue = this.quoteForm.value;
    const authors = this.selectedAuthors().map(a => ({
      applicationUserId: a.isUser ? a.id : null,
      customName: a.isUser ? null : a.name
    }));

    const result = {
      text: formValue.text,
      dateSaid: formValue.dateSaid.toISOString(),
      authors: authors,
      visibleToRoles: formValue.visibleToRoles,
      sendNotification: formValue.sendNotification
    };

    this.ref.close(result);
  }
}
