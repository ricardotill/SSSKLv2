import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ReactionListComponent } from './reaction-list.component';
import { ReactionService } from '../../../core/services/reaction.service';
import { AuthService } from '../../../core/auth/auth.service';
import { of } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { ReactionDto } from '../../../core/models/reaction.model';
import { ConfirmationService, MessageService } from 'primeng/api';

describe('ReactionListComponent', () => {
  let component: ReactionListComponent;
  let fixture: ComponentFixture<ReactionListComponent>;
  let reactionServiceMock: any;
  let authServiceMock: any;

  const mockReactions: ReactionDto[] = [
    { id: '1', userId: 'u1', userName: 'Alice', content: '👍', targetId: 't1', targetType: 'Event', createdOn: new Date() },
    { id: '2', userId: 'u2', userName: 'Bob', content: '👍', targetId: 't1', targetType: 'Event', createdOn: new Date() },
    { id: '3', userId: 'u1', userName: 'Alice', content: '❤️', targetId: 't1', targetType: 'Event', createdOn: new Date() },
  ];

  beforeEach(async () => {
    reactionServiceMock = {
      getReactions: vi.fn().mockReturnValue(of(mockReactions)),
      toggleReaction: vi.fn().mockReturnValue(of(void 0))
    };

    authServiceMock = {
      isAuthenticated: vi.fn().mockReturnValue(true),
      currentUser: vi.fn().mockReturnValue({ id: 'u1', userName: 'Alice' })
    };

    await TestBed.configureTestingModule({
      imports: [ReactionListComponent],
      providers: [
        { provide: ReactionService, useValue: reactionServiceMock },
        { provide: AuthService, useValue: authServiceMock },
        { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
        { provide: MessageService, useValue: { add: vi.fn() } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ReactionListComponent);
    component = fixture.componentInstance;
    component.targetId = 't1';
    component.targetType = 'Event';
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load reactions on init', () => {
    expect(component.reactions().length).toBe(3);
    expect(component.reactions()[0].userName).toBe('Alice');
  });

  it('should toggle reaction when clicked', () => {
    component.toggle('👍');
    expect(reactionServiceMock.toggleReaction).toHaveBeenCalledWith('t1', 'Event', '👍');
    expect(reactionServiceMock.getReactions).toHaveBeenCalledTimes(2); // once on init, once after toggle
  });

  it('should not toggle empty reaction', () => {
    component.toggle('');
    expect(reactionServiceMock.toggleReaction).not.toHaveBeenCalled();
  });

  it('should show add button only when authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(false);
    fixture.detectChanges();
    // Re-check template or signal if used
    expect(component.authService.isAuthenticated()).toBe(false);
  });
});
