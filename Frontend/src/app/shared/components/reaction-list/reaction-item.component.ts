import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactionDto } from '../../../core/models/reaction.model';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ResolveApiUrlPipe } from '../../pipes/resolve-api-url.pipe';
import { UserProfileDrawerService } from '../../../core/services/user-profile-drawer.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ReactionService } from '../../../core/services/reaction.service';
import { TextareaModule } from 'primeng/textarea';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-reaction-item',
  standalone: true,
  imports: [CommonModule, AvatarModule, ButtonModule, TooltipModule, ResolveApiUrlPipe, TextareaModule, FormsModule],
  template: `
    <div class="flex gap-3 py-3 px-2 rounded-xl transition-colors hover:bg-surface-50 dark:hover:bg-surface-800/50">
      <p-avatar 
        [image]="reaction.profilePictureUrl | resolveApiUrl" 
        [label]="!reaction.profilePictureUrl ? reaction.userName[0] : ''"
        shape="circle" 
        size="normal"
        class="cursor-pointer"
        (click)="drawerService.open(reaction.userId)"
      />
      
      <div class="flex flex-col flex-1 gap-1">
        <div class="flex items-center gap-2">
          <span class="font-bold text-sm cursor-pointer hover:underline" (click)="drawerService.open(reaction.userId)">
            @{{ reaction.userName }}
          </span>
          <span class="text-xs text-muted-color">
            {{ reaction.createdOn | date:'short' }}
          </span>
        </div>
        
        <div class="text-sm leading-relaxed text-surface-700 dark:text-surface-300">
          {{ reaction.content }}
        </div>
        
        <div class="flex items-center gap-4 mt-1">
          <div class="flex items-center gap-1 group">
            <p-button 
              [icon]="hasDone('👍') ? 'pi pi-thumbs-up-fill' : 'pi pi-thumbs-up'" 
              [text]="true" 
              [rounded]="true" 
              [severity]="hasDone('👍') ? 'primary' : 'secondary'" 
              styleClass="w-7 h-7"
              (onClick)="onReact.emit('👍')"
              [pTooltip]="getUsersFor('👍')"
              tooltipPosition="top"
            />
            @if (getReactionCount('👍') > 0) {
              <span class="text-xs font-semibold opacity-60">{{ getReactionCount('👍') }}</span>
            }
          </div>
          
          <div class="flex items-center gap-1 group">
            <p-button 
              [icon]="hasDone('❤️') ? 'pi pi-heart-fill' : 'pi pi-heart'" 
              [text]="true" 
              [rounded]="true" 
              [severity]="hasDone('❤️') ? 'danger' : 'secondary'" 
              styleClass="w-7 h-7"
              (onClick)="onReact.emit('❤️')"
              [pTooltip]="getUsersFor('❤️')"
              tooltipPosition="top"
            />
            @if (getReactionCount('❤️') > 0) {
              <span class="text-xs font-semibold opacity-60">{{ getReactionCount('❤️') }}</span>
            }
          </div>

          @if (authService.isAuthenticated()) {
            <p-button 
              [label]="activeReplyTarget()?.id === reaction.id ? 'Annuleren' : 'Reageren'" 
              [text]="true" 
              severity="secondary" 
              size="small"
              styleClass="text-xs p-1 h-auto opacity-60 hover:opacity-100"
              (onClick)="toggleReply(reaction)"
            />
          }
          
          @if (canDelete(reaction)) {
            <p-button 
              icon="pi pi-trash" 
              [text]="true" 
              severity="danger" 
              size="small"
              styleClass="text-xs p-1 h-auto opacity-50 hover:opacity-100"
              (onClick)="confirmAndRemove(reaction)"
              pTooltip="Verwijderen"
              tooltipPosition="top"
            />
          }
        </div>

        <!-- Inline Reply Input (For Main Reaction) -->
        @if (activeReplyTarget()?.id === reaction.id) {
          <ng-container *ngTemplateOutlet="replyBox; context: { target: reaction }"></ng-container>
        }

        <ng-template #replyBox let-target="target">
          <div class="mt-4 flex gap-3 animate-in slide-in-from-top duration-200" [class.ml-8]="target.id !== reaction.id">
            <p-avatar 
              [image]="authService.currentUser()?.profilePictureUrl | resolveApiUrl" 
              [label]="!authService.currentUser()?.profilePictureUrl ? authService.currentUser()?.userName?.substring(0,1) : undefined"
              shape="circle" 
              size="normal"
              class="w-7 h-7"
            />
            <div class="flex-1 flex flex-col gap-2">
              <div class="text-[11px] font-medium opacity-60 flex items-center gap-1 -mb-1">
                <i class="pi pi-reply" style="font-size: 7px; transform: scale(0.9); opacity: 0.8;"></i>
                Reageren op <span class="text-primary-500 font-bold">@{{ target.userName }}</span>
              </div>
              <textarea 
                pInputTextarea 
                [autoResize]="true"
                rows="1"
                [(ngModel)]="replyContent" 
                [placeholder]="'Schrijf een reactie...'" 
                class="w-full border-b border-surface-200 dark:border-surface-700 bg-transparent focus:border-primary-500 p-0 text-sm transition-colors mt-1"
                (keydown.enter)="$event.preventDefault(); submitReply()"
              ></textarea>
              <div class="flex justify-end gap-2">
                <p-button label="Annuleren" [text]="true" severity="secondary" styleClass="p-button-sm" (onClick)="activeReplyTarget.set(null)" />
                <p-button label="Reageren" [disabled]="!replyContent.trim()" styleClass="p-button-sm rounded-full" (onClick)="submitReply()" />
              </div>
            </div>
          </div>
        </ng-template>

        <!-- Nested Reactions (Flat thread) -->
        @if (reaction.reactions && reaction.reactions.length > 0) {
          <div class="mt-2 pl-4 border-l-2 border-surface-200 dark:border-surface-700 flex flex-col gap-4">
             @for (child of reaction.reactions; track child.id) {
               @if (child.content !== '👍' && child.content !== '❤️') {
                 <div class="flex items-start gap-2 py-1">
                   <p-avatar 
                     [image]="child.profilePictureUrl | resolveApiUrl" 
                     [label]="!child.profilePictureUrl ? child.userName[0] : ''"
                     shape="circle" 
                     styleClass="w-6 h-6 mt-0.5"
                     class="cursor-pointer"
                     (click)="drawerService.open(child.userId)"
                   />
                   <div class="flex flex-col flex-1 gap-0.5">
                     <div class="flex items-center gap-2">
                       <span class="text-xs font-bold leading-none cursor-pointer hover:underline" (click)="drawerService.open(child.userId)">
                         @{{ child.userName }}
                       </span>
                       @if (child.targetUserName) {
                         <span class="text-[10px] opacity-50 flex items-center gap-1">
                           <i class="pi pi-arrow-right" style="font-size: 6px; opacity: 0.6;"></i>
                           <span class="font-medium text-primary-500/80">@{{ child.targetUserName }}</span>
                         </span>
                       }
                       <span class="text-[10px] opacity-40">{{ child.createdOn | date:'short' }}</span>
                     </div>
                     <span class="text-[11px] leading-tight text-surface-700 dark:text-surface-300">{{ child.content }}</span>
                     
                     <div class="flex gap-2">
                       @if (authService.isAuthenticated()) {
                         <p-button 
                           [label]="activeReplyTarget()?.id === child.id ? 'Annuleren' : 'Reageren'" 
                           [text]="true" 
                           severity="secondary" 
                           size="small"
                           styleClass="text-xs p-1 h-auto opacity-50 hover:opacity-100 mt-1 w-fit"
                           (onClick)="toggleReply(child)"
                         />
                       }

                       @if (canDelete(child)) {
                         <p-button 
                           icon="pi pi-trash" 
                           [text]="true" 
                           severity="danger" 
                           size="small"
                           styleClass="text-xs p-1 h-auto opacity-50 hover:opacity-100 mt-1 w-fit"
                           (onClick)="confirmAndRemove(child)"
                           pTooltip="Verwijderen"
                           tooltipPosition="top"
                         />
                       }
                     </div>
                   </div>
                 </div>
                 
                 <!-- Inline Reply Input (For Nested Reaction) -->
                 @if (activeReplyTarget()?.id === child.id) {
                   <ng-container *ngTemplateOutlet="replyBox; context: { target: child }"></ng-container>
                 }
               }
             }
          </div>
        }
      </div>
    </div>
  `
})
export class ReactionItemComponent {
  @Input({ required: true }) reaction!: ReactionDto;
  @Output() onReact = new EventEmitter<string>();
  @Output() onReply = new EventEmitter<void>();

  private readonly reactionService = inject(ReactionService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly messageService = inject(MessageService);
  public readonly drawerService = inject(UserProfileDrawerService);
  public readonly authService = inject(AuthService);

  activeReplyTarget = signal<ReactionDto | null>(null);
  replyContent = '';

  toggleReply(target: ReactionDto): void {
    if (this.activeReplyTarget()?.id === target.id) {
      this.activeReplyTarget.set(null);
    } else {
      this.activeReplyTarget.set(target);
      this.replyContent = '';
    }
  }

  submitReply(): void {
    const target = this.activeReplyTarget();
    if (!target || !this.replyContent.trim()) return;

    this.reactionService.toggleReaction(target.id, 'Reaction', this.replyContent.trim()).subscribe(() => {
      this.activeReplyTarget.set(null);
      this.onReply.emit(); // Notify parent to reload
    });
  }

  canDelete(reaction: ReactionDto): boolean {
    const user = this.authService.currentUser();
    if (!user) return false;
    
    return user.roles?.includes('Admin') || reaction.userId === user.id;
  }

  confirmAndRemove(reaction: ReactionDto): void {
    this.confirmationService.confirm({
      header: 'Bevestig verwijdering',
      message: 'Weet je zeker dat je deze reactie wilt verwijderen? Alle onderliggende reacties worden ook verwijderd.',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Verwijderen',
      rejectLabel: 'Annuleren',
      acceptButtonStyleClass: 'p-button-danger p-button-sm',
      rejectButtonStyleClass: 'p-button-text p-button-secondary p-button-sm',
      accept: () => {
        this.reactionService.deleteReaction(reaction.id).subscribe(() => {
          this.messageService.add({ 
            severity: 'success', 
            summary: 'Verwijderd', 
            detail: 'Reactie succesvol verwijderd' 
          });
          this.onReply.emit(); // Trigger refresh
        });
      }
    });
  }

  getReactionCount(content: string): number {
    return this.reaction.reactions?.filter(r => r.content === content).length ?? 0;
  }

  hasDone(content: string): boolean {
    const userId = this.authService.currentUser()?.id;
    return this.reaction.reactions?.some(r => r.content === content && r.userId === userId) ?? false;
  }

  getUsersFor(content: string): string {
    const names = this.reaction.reactions?.filter(r => r.content === content).map(r => r.userName) ?? [];
    if (names.length === 0) return '';
    return names.join(', ');
  }
}
