import { TestBed, ComponentFixture } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MessageService, ConfirmationService } from 'primeng/api';
import { vi } from 'vitest';
import UsersComponent from './users.component';
import { ApplicationUserService } from '../../users/services/application-user.service';
import { RoleService } from '../services/role.service';
import { ApplicationUserDto } from '../../../core/models/application-user.model';

describe('UsersComponent (rendered)', () => {
  let fixture: ComponentFixture<UsersComponent>;
  let component: UsersComponent;
  let userService: any;
  let messageService: { add: ReturnType<typeof vi.fn> };
  let confirmationService: any;

  const user: ApplicationUserDto = {
    id: 'u1',
    userName: 'jan',
    fullName: 'Jan Jansen',
    saldo: 12.5,
    lastOrdered: '2026-01-01T10:00:00Z',
    profilePictureUrl: 'pic.png',
    roles: ['User'],
  } as ApplicationUserDto;

  const details = {
    ...user,
    email: 'jan@example.com',
    emailConfirmed: true,
    phoneNumber: null,
    phoneNumberConfirmed: false,
    name: 'Jan',
    surname: null,
    roles: ['User'],
  };

  const job = (status: string, processed = 5) => ({
    id: 'job-1',
    status,
    totalUsers: 10,
    processedUsers: processed,
    failedUsers: status === 'Failed' ? 1 : 0,
    startedAt: '2026-01-01T00:00:00Z',
    startedByUserId: 'admin',
  });

  beforeEach(async () => {
    userService = {
      getAdminUsers: vi.fn(() => of({ items: [user], totalCount: 1 })),
      getUser: vi.fn(() => of(details)),
      updateUser: vi.fn(() => of(void 0)),
      deleteUser: vi.fn(() => of(void 0)),
      deleteProfilePicture: vi.fn(() => of(void 0)),
      recalculateUserStats: vi.fn(() => of({})),
      startRecalculateAllStats: vi.fn(() => of(job('Pending', 0))),
      getLatestRecalculateAllStatsStatus: vi.fn(() => throwError(() => new Error('none'))),
      getRecalculateAllStatsStatus: vi.fn(() => of(job('Completed', 10))),
    };
    messageService = { add: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [UsersComponent, NoopAnimationsModule],
      providers: [
        { provide: ApplicationUserService, useValue: userService },
        { provide: RoleService, useValue: { getAdminRoles: vi.fn(() => of([{ name: 'Admin' }, { name: 'User' }])) } },
        { provide: MessageService, useValue: messageService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UsersComponent);
    component = fixture.componentInstance;
    // ConfirmationService is provided by the component itself; auto-accept every confirmation.
    confirmationService = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmationService, 'confirm').mockImplementation((c: any) => {
      c.accept();
      return confirmationService;
    });
    fixture.detectChanges();
  });

  afterEach(() => {
    component?.ngOnDestroy();
    vi.useRealTimers();
  });

  it('loads and renders users and roles', () => {
    expect(component.users()).toEqual([user]);
    expect(component.totalRecords()).toBe(1);
    expect(component.availableRoles()).toEqual([
      { label: 'Admin', value: 'Admin' },
      { label: 'User', value: 'User' },
    ]);
    expect(fixture.nativeElement.textContent).toContain('jan');
  });

  it('reports a failed user load', () => {
    userService.getAdminUsers.mockReturnValue(throwError(() => new Error('x')));
    component.loadUsers();
    expect(component.loading()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('opens the edit dialog with the user details and saves changes', () => {
    component.openEditDialog(user);
    fixture.detectChanges();

    expect(component.editDialogVisible()).toBe(true);
    expect(component.editForm.getRawValue().email).toBe('jan@example.com');
    expect(component.editingUserProfilePictureUrl()).toBe('pic.png');

    component.editForm.patchValue({ password: 'Secret123!' });
    component.saveEdit();

    expect(userService.updateUser).toHaveBeenCalledWith(
      'u1',
      expect.objectContaining({ userName: 'jan', emailConfirmed: true, password: 'Secret123!', roles: ['User'] }),
    );
    expect(component.editDialogVisible()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('omits an empty password on save', () => {
    component.openEditDialog(user);
    component.saveEdit();
    expect(userService.updateUser.mock.calls[0][1]).not.toHaveProperty('password');
  });

  it('reports a failed save', () => {
    userService.updateUser.mockReturnValue(throwError(() => new Error('x')));
    component.openEditDialog(user);
    component.saveEdit();
    expect(component.saving()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('does not save without a selected user', () => {
    component.saveEdit();
    expect(userService.updateUser).not.toHaveBeenCalled();
  });

  it('closes the edit dialog when the details fail to load', () => {
    userService.getUser.mockReturnValue(throwError(() => new Error('x')));
    component.openEditDialog(user);
    expect(component.editDialogVisible()).toBe(false);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('deletes a user after confirmation', () => {
    component.confirmDelete(user);
    expect(userService.deleteUser).toHaveBeenCalledWith('u1');
    expect(component.deletingUserId()).toBeNull();
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));
  });

  it('reports a failed delete', () => {
    userService.deleteUser.mockReturnValue(throwError(() => new Error('x')));
    component.deleteUser('u1');
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('removes the profile picture of the edited user', () => {
    component.openEditDialog(user);
    component.confirmRemoveProfilePicture();
    expect(userService.deleteProfilePicture).toHaveBeenCalledWith('u1');
    expect(component.editingUserProfilePictureUrl()).toBeNull();
  });

  it('reports a failed profile picture removal and ignores it without a user', () => {
    component.removeProfilePicture();
    expect(userService.deleteProfilePicture).not.toHaveBeenCalled();

    userService.deleteProfilePicture.mockReturnValue(throwError(() => new Error('x')));
    component.openEditDialog(user);
    component.removeProfilePicture();
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('recalculates the stats of one user', () => {
    component.recalculateStats('u1');
    expect(component.recalculatingUserId()).toBeNull();
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success' }));

    userService.recalculateUserStats.mockReturnValue(throwError(() => new Error('x')));
    component.recalculateStats('u1');
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('starts a recalculation of all stats and polls until it completes', () => {
    vi.useFakeTimers();
    component.confirmRecalculateAllStats();
    fixture.detectChanges();

    expect(component.recalculateAllJob()?.status).toBe('Pending');
    expect(component.isRecalculateAllRunning()).toBe(true);
    expect(component.recalculateAllProgress()).toBe(0);
    expect(component.recalculateAllStatusMessage()).toBeTruthy();

    vi.advanceTimersByTime(3000);
    fixture.detectChanges();

    expect(component.recalculateAllJob()?.status).toBe('Completed');
    expect(component.recalculateAllProgress()).toBe(100);
    expect(component.recalculateAllStatusMessage()).toBeTruthy();
    expect(userService.getAdminUsers).toHaveBeenCalledTimes(2);
  });

  it('resumes a recalculation that is already running', () => {
    userService.startRecalculateAllStats.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: job('Running') })),
    );
    component.confirmRecalculateAllStats();
    fixture.detectChanges();

    expect(component.recalculateAllJob()?.status).toBe('Running');
    expect(component.recalculateAllProgress()).toBe(50);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'info' }));
  });

  it('reports a failed recalculation start', () => {
    userService.startRecalculateAllStats.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );
    component.confirmRecalculateAllStats();
    expect(component.recalculateAllJob()).toBeNull();
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });

  it('shows a failed recalculation job and reports polling errors', () => {
    component.recalculateAllJob.set(job('Failed') as any);
    fixture.detectChanges();
    expect(component.isRecalculateAllRunning()).toBe(false);
    expect(component.recalculateAllStatusMessage()).toBeTruthy();

    vi.useFakeTimers();
    userService.getRecalculateAllStatsStatus.mockReturnValue(throwError(() => new Error('x')));
    (component as any).pollRecalculateAllStatus('job-1');
    vi.advanceTimersByTime(3000);
    expect(messageService.add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error' }));
  });
});
