import { TestBed, ComponentFixture } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { ConfirmationService, MessageService } from 'primeng/api';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { vi } from 'vitest';
import { ReactionItemComponent } from './reaction-item.component';
import { ReactionService } from '../../../core/services/reaction.service';
import { AuthService } from '../../../core/auth/auth.service';
import { UserProfileDrawerService } from '../../../core/services/user-profile-drawer.service';
import { ReactionDto } from '../../../core/models/reaction.model';

describe('ReactionItemComponent', () => {
  let fixture: ComponentFixture<ReactionItemComponent>;
  let component: ReactionItemComponent;
  let reactionService: any;
  let messageService: { add: ReturnType<typeof vi.fn> };
  let confirmationService: { confirm: ReturnType<typeof vi.fn> };
  let drawerService: { open: ReturnType<typeof vi.fn> };
  let currentUser: ReturnType<typeof signal<any>>;

  const child = (id: string, content: string, userId = 'u2', extra: Partial<ReactionDto> = {}): ReactionDto =>
    ({
      id,
      userId,
      userName: userId === 'me' ? 'me' : 'piet',
      content,
      targetId: 'r1',
      targetType: 'Reaction',
      createdOn: '2026-01-01T10:00:00Z',
      reactions: [],
      ...extra,
    }) as ReactionDto;

  const reaction: ReactionDto = {
    id: 'r1',
    userId: 'u2',
    userName: 'piet',
    profilePictureUrl: null,
    content: 'Mooi!',
    targetId: 'q1',
    targetType: 'Quote',
    createdOn: '2026-01-01T09:00:00Z',
    reactions: [
      child('c1', '👍', 'me'),
      child('c2', '👍', 'u3'),
      child('c3', '❤️', 'u3'),
      child('c4', 'Eens', 'me', { targetUserName: 'piet', profilePictureUrl: 'p.png' }),
      child('c5', 'Oneens', 'u3'),
    ],
  } as unknown as ReactionDto;

  beforeEach(async () => {
    reactionService = {
      toggleReaction: vi.fn(() => of({})),
      deleteReaction: vi.fn(() => of(void 0)),
    };
    messageService = { add: vi.fn() };
    confirmationService = { confirm: vi.fn((c: any) => c.accept()) };
    drawerService = { open: vi.fn() };
    currentUser = signal<any>({ id: 'me', userName: 'me', roles: ['User'], profilePictureUrl: null });

    await TestBed.configureTestingModule({
      imports: [ReactionItemComponent, NoopAnimationsModule],
      providers: [
        { provide: ReactionService, useValue: reactionService },
        { provide: ConfirmationService, useValue: confirmationService },
        { provide: MessageService, useValue: messageService },
        { provide: UserProfileDrawerService, useValue: drawerService },
        { provide: AuthService, useValue: { currentUser, isAuthenticated: () => !!currentUser() } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReactionItemComponent);
    component = fixture.componentInstance;
    component.reaction = reaction;
    fixture.detectChanges();
  });

  it('renders the reaction with its replies but not its emoji reactions', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Mooi!');
    expect(text).toContain('Eens');
    expect(text).toContain('Oneens');
    expect(text).toContain('@piet');
  });

  it('counts emoji reactions and who gave them', () => {
    expect(component.getReactionCount('👍')).toBe(2);
    expect(component.getReactionCount('❤️')).toBe(1);
    expect(component.hasDone('👍')).toBe(true);
    expect(component.hasDone('❤️')).toBe(false);
    expect(component.getUsersFor('👍')).toBe('me, piet');
    expect(component.getUsersFor('🎉')).toBe('');
  });

  it('handles a reaction without replies', () => {
    component.reaction = { ...reaction, reactions: undefined } as unknown as ReactionDto;
    expect(component.getReactionCount('👍')).toBe(0);
    expect(component.hasDone('👍')).toBe(false);
    expect(component.getUsersFor('👍')).toBe('');
  });

  it('lets owners and admins delete', () => {
    expect(component.canDelete(reaction)).toBe(false);
    expect(component.canDelete(reaction.reactions![3])).toBe(true);

    currentUser.set({ id: 'admin', roles: ['Admin'] });
    expect(component.canDelete(reaction)).toBe(true);

    currentUser.set(null);
    expect(component.canDelete(reaction)).toBe(false);
  });

  it('toggles the reply box and submits a reply', () => {
    const replied = vi.fn();
    component.onReply.subscribe(replied);

    component.toggleReply(reaction);
    fixture.detectChanges();
    expect(component.activeReplyTarget()).toBe(reaction);
    expect(fixture.nativeElement.textContent).toContain('Reageren op');

    component.submitReply();
    expect(reactionService.toggleReaction).not.toHaveBeenCalled();

    component.replyContent = '  Top  ';
    component.submitReply();

    expect(reactionService.toggleReaction).toHaveBeenCalledWith('r1', 'Reaction', 'Top');
    expect(component.activeReplyTarget()).toBeNull();
    expect(replied).toHaveBeenCalled();
  });

  it('opens and closes the reply box for a nested reply', () => {
    const nested = reaction.reactions![3];
    component.toggleReply(nested);
    fixture.detectChanges();
    expect(component.activeReplyTarget()).toBe(nested);

    component.toggleReply(nested);
    expect(component.activeReplyTarget()).toBeNull();
  });

  it('deletes a reaction after confirmation', () => {
    const replied = vi.fn();
    component.onReply.subscribe(replied);

    component.confirmAndRemove(reaction.reactions![3]);

    expect(confirmationService.confirm).toHaveBeenCalled();
    expect(reactionService.deleteReaction).toHaveBeenCalledWith('c4');
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
    expect(replied).toHaveBeenCalled();
  });

  it('opens the profile of the author', () => {
    fixture.nativeElement.querySelector('span.font-bold').click();
    expect(drawerService.open).toHaveBeenCalledWith('u2');
  });

  it('hides reply actions for anonymous visitors', () => {
    currentUser.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Reageren');
  });
});
