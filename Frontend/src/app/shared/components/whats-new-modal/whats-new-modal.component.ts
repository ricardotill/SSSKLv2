import { Component, ChangeDetectionStrategy, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { WhatsNewService } from '../../../features/admin/services/whats-new.service';
import { LanguageService } from '../../../core/services/language.service';
import { ProcessedContentPipe } from '../../pipes/processed-content.pipe';

@Component({
  selector: 'app-whats-new-modal',
  standalone: true,
  imports: [CommonModule, DialogModule, ButtonModule, ProcessedContentPipe],
  template: `
    <p-dialog 
      [(visible)]="isVisible" 
      [modal]="true" 
      [header]="ls.t().whats_new_modal_title"
      [style]="{ width: '90vw', maxWidth: '650px' }"
      [draggable]="false"
      [resizable]="false"
      [closable]="true"
      (onHide)="onClose()"
      styleClass="rounded-2xl overflow-hidden shadow-2xl"
      contentStyleClass="p-0">
      
      <div class="flex flex-col max-h-[70vh]">
        <!-- Scrollable content area -->
        <div class="overflow-y-auto px-6 py-6 custom-scrollbar">
          <div 
            class="rich-text-content text-surface-700 dark:text-surface-300 font-body"
            style="word-wrap: break-word; overflow-wrap: break-word; word-break: normal; white-space: normal;"
            [innerHTML]="content() | processedContent">
          </div>
        </div>

        <!-- Sticky Footer -->
        <div class="p-6 border-t border-surface-200 dark:border-surface-700 bg-surface-50 dark:bg-surface-800/50 flex justify-end">
          <p-button 
            [label]="ls.t().awesome" 
            icon="pi pi-check" 
            (onClick)="onClose()" 
            styleClass="p-button-primary p-button-rounded px-8 font-bold">
          </p-button>
        </div>
      </div>
    </p-dialog>
  `,
  styles: [`
    :host ::ng-deep .rich-text-content {
      line-height: 1.6;
    }
    :host ::ng-deep .rich-text-content * {
      word-break: normal !important;
      overflow-wrap: break-word !important;
      white-space: normal !important;
    }
    :host ::ng-deep .rich-text-content h1, 
    :host ::ng-deep .rich-text-content h2, 
    :host ::ng-deep .rich-text-content h3 {
      font-weight: 700;
      margin-top: 1.5rem;
      margin-bottom: 0.75rem;
      color: var(--p-primary-color);
    }
    :host ::ng-deep .rich-text-content p {
      margin-bottom: 1rem;
    }
    :host ::ng-deep .rich-text-content ul, 
    :host ::ng-deep .rich-text-content ol {
      margin-bottom: 1rem;
      padding-left: 1.5rem;
    }
    :host ::ng-deep .rich-text-content ul {
      list-style-type: disc;
    }
    :host ::ng-deep .rich-text-content ol {
      list-style-type: decimal;
    }
    :host ::ng-deep .rich-text-content li {
      margin-bottom: 0.5rem;
    }
    .custom-scrollbar::-webkit-scrollbar {
      width: 6px;
    }
    .custom-scrollbar::-webkit-scrollbar-track {
      background: transparent;
    }
    .custom-scrollbar::-webkit-scrollbar-thumb {
      background: var(--p-surface-300);
      border-radius: 10px;
    }
    .dark .custom-scrollbar::-webkit-scrollbar-thumb {
      background: var(--p-surface-600);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WhatsNewModalComponent {
  private whatsNewService = inject(WhatsNewService);
  ls = inject(LanguageService);
  
  get isVisible() {
    return this.whatsNewService.isVisible();
  }
  
  set isVisible(value: boolean) {
    if (!value) {
      this.whatsNewService.markAsSeen();
    }
  }

  content = this.whatsNewService.content;

  onClose() {
    this.whatsNewService.markAsSeen();
  }
}
