import { Component, Input, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactionService } from '../../../core/services/reaction.service';
import { ReactionDto, ReactionTargetType } from '../../../core/models/reaction.model';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TextareaModule } from 'primeng/textarea';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/auth/auth.service';
import { UserProfileDrawerService } from '../../../core/services/user-profile-drawer.service';
import { AvatarModule } from 'primeng/avatar';
import { ResolveApiUrlPipe } from '../../pipes/resolve-api-url.pipe';

import { ReactionItemComponent } from './reaction-item.component';

@Component({
  selector: 'app-reaction-list',
  standalone: true,
  imports: [
    CommonModule, 
    ButtonModule, 
    TooltipModule, 
    TextareaModule, 
    FormsModule,
    AvatarModule,
    ResolveApiUrlPipe,
    ReactionItemComponent
  ],
  template: `
    <div class="flex flex-col gap-6">
      <!-- Comment Input Area -->
      @if (authService.isAuthenticated()) {
        <div class="flex gap-4 p-4 bg-surface-50 dark:bg-surface-800/50 rounded-2xl border border-surface-200 dark:border-surface-700">
          <p-avatar 
            [image]="authService.currentUser()?.profilePictureUrl | resolveApiUrl" 
            [label]="!authService.currentUser()?.profilePictureUrl ? authService.currentUser()?.userName?.substring(0,1) : undefined"
            shape="circle" 
            size="normal"
            class="hidden sm:block"
          />
          <div class="flex-1 flex flex-col gap-3">
            <textarea 
              pInputTextarea 
              [autoResize]="true"
              rows="1"
              [(ngModel)]="customReaction" 
              placeholder="Deel je gedachten..." 
              class="w-full border-none bg-transparent focus:ring-0 p-0 text-sm overflow-hidden"
              (keydown.enter)="$event.preventDefault(); toggle(customReaction); customReaction = ''"
            ></textarea>
            
            <div class="flex justify-between items-center border-t border-surface-100 dark:border-surface-700 pt-2 -mx-1">
              <div class="flex gap-1">
                 @for (quick of filteredQuickReactions().slice(0, 5); track quick) {
                   <p-button 
                     [label]="quick" 
                     [text]="true" 
                     (onClick)="toggle(quick)" 
                     styleClass="p-1 h-8 w-8 text-lg hover:scale-110 transition-transform" 
                   />
                 }
              </div>
              <p-button 
                label="Plaatsen" 
                [disabled]="!customReaction.trim()" 
                (onClick)="toggle(customReaction); customReaction = ''" 
                styleClass="p-button-sm rounded-full px-4"
              />
            </div>
          </div>
        </div>
      }

      <!-- Feed -->
      <div class="flex flex-col divide-y divide-surface-100 dark:divide-surface-800">
        @for (reaction of reactions(); track reaction.id) {
          <app-reaction-item 
            [reaction]="reaction" 
            (onReact)="toggleOnReaction(reaction, $event)"
            (onReply)="loadReactions()"
          />
        }
        @if (reactions().length === 0) {
          <div class="py-12 flex flex-col items-center justify-center opacity-40 grayscale">
             <i class="pi pi-comments text-4xl mb-4"></i>
             <span class="text-sm italic">Nog geen reacties. Wees de eerste!</span>
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ReactionListComponent implements OnInit {
  @Input({ required: true }) targetId!: string;
  @Input({ required: true }) targetType!: ReactionTargetType;

  private readonly reactionService = inject(ReactionService);
  public readonly authService = inject(AuthService);
  public readonly drawerService = inject(UserProfileDrawerService);

  reactions = signal<ReactionDto[]>([]);
  customReaction = '';
  quickReactions = ['👍', '❤️', '😂', '🎉', '🔥', '🤔', '😢'];
  filteredQuickReactions = computed(() => {
    if (this.targetType === 'Quote') {
      return this.quickReactions.filter(r => r !== '❤️');
    }
    return this.quickReactions;
  });

  replyingTo: ReactionDto | null = null;

  ngOnInit(): void {
    if (this.targetId) {
      this.loadReactions();
    }
  }

  loadReactions(): void {
    this.reactionService.getReactions(this.targetId, this.targetType).subscribe(res => {
      this.reactions.set(res);
    });
  }

  toggle(content: string): void {
    if (!content || !content.trim()) return;
    
    const tid = this.replyingTo ? this.replyingTo.id : this.targetId;
    const ttype = this.replyingTo ? 'Reaction' : this.targetType;

    this.reactionService.toggleReaction(tid, ttype, content.trim()).subscribe(() => {
      this.loadReactions();
      this.replyingTo = null;
    });
  }

  toggleOnReaction(parent: ReactionDto, content: string): void {
    this.reactionService.toggleReaction(parent.id, 'Reaction', content).subscribe(() => {
      this.loadReactions();
    });
  }

  openReply(event: any, reaction: ReactionDto, op: any): void {
    // Legacy popover logic removed in favor of inline replies
  }
}
